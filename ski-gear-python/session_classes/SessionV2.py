import os
import sqlite3
import time
from datetime import datetime, timedelta, timezone
import Fall
import numpy as np
import pyqtgraph as pg

from .BaseSession import BaseSession
from data_classes.session_header import SessionHeader
from data_classes.GPSData import GPSData
from data_classes.sensors_data_1khzStruct import SensorsData1KHZStruct
from data_classes.sensors_data_100hzStruct import SensorsData100HZStruct
from Graph import NoRightZoomViewBox, Graph

class SessionV2(BaseSession):

    MSG_SESS = 4
    MSG_1KHZ = 13
    MSG_100HZ = 14

    MAX_GRAPHS = 5
    MAX_SENSORS = 5

    gyro_index = 1
    speed_index = 2
    mainAcc_index = 0
    gravity_index = 4
    pose_index = 3

    TITLE_SPEED = "Speed\n[km/h]"
    TITLE_GYRO = "Gyro\n[rad/s]"
    TITLE_MAIN = "Main\n[g]"
    TITLE_GRAVITY = "Gravity\n[g]"
    TITLE_POSE = "Pose\n[rad]"
    
    def __init__(self, device_id: int, session_id: int, filename: str, right_panel=None):
        super().__init__(nofgraphs=5, nofsensors=5)
        self.session_version = 2 # ancora da capire perchè esiste una sessione v1 e v2
        self.right_panel = right_panel
        self.device_id = device_id
        self.session_id = session_id
        self.session_file_name = filename
        self.header: SessionHeader | None = None
        
        self.right_panel.resetPathCoords()

        dbfile = self._prepare_db(filename)
        self.conn = sqlite3.connect(dbfile)
        self.conn.execute("PRAGMA journal_mode=WAL;")

        self._create_schema()

        self.fast_insert_sql = (
            "INSERT INTO fastSensors "
            "(dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, activation, Timestamp) "
            "VALUES (?,?,?,?,?,?,?,?,?)"
        )
        self.slow_insert_sql = (
            "INSERT INTO slowSensors "
            "(dataIndex, mag_x, mag_y, mag_z, "
            " rot_x, rot_y, rot_z, "
            " grav_x, grav_y, grav_z, activation, Timestamp) "
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?)"
        )
        self.gps_insert_sql = (
            "INSERT INTO gpssensors (dataIndex, latitude, longitude, speed, Timestamp) "
            "VALUES (?,?,?,?,?)"
        )

        self.cur_fast = self.conn.cursor()
        self.cur_slow = self.conn.cursor()
        self.cur_gps = self.conn.cursor()
        
        self.MinIndex = 0
        self.MaxIndex = 0

    def _create_schema(self):
        if self.conn is None:
            raise ValueError("Database connection is not initialized.")
        cur = self.conn.cursor()
        cur.executescript("""
            CREATE TABLE IF NOT EXISTS gpssensors (
                Timestamp DATETIME DEFAULT null,
                dataIndex INTEGER UNIQUE ON CONFLICT REPLACE,
                latitude,
                longitude,
                speed
            );

            CREATE TABLE IF NOT EXISTS slowSensors (
                Timestamp DATETIME DEFAULT null,
                dataIndex INTEGER UNIQUE ON CONFLICT REPLACE,
                mag_x,
                mag_y,
                mag_z,
                rot_x,
                rot_y,
                rot_z,
                grav_x,
                grav_y,
                grav_z,
                activation
            );

            CREATE TABLE IF NOT EXISTS fastSensors (
                Timestamp DATETIME DEFAULT null,
                dataIndex INTEGER UNIQUE ON CONFLICT REPLACE,
                acc_x,
                acc_y,
                acc_z,
                gyro_x,
                gyro_y,
                gyro_z,
                activation
            );

            CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp DATETIME DEFAULT null,
                type TEXT,
                description TEXT
            );

            CREATE TABLE IF NOT EXISTS notes (
                type TEXT,
                description TEXT
            );

            CREATE INDEX IF NOT EXISTS slowTimestampIdx ON slowSensors(Timestamp);
            CREATE INDEX IF NOT EXISTS slowDataIdx ON slowSensors(dataIndex);
            CREATE INDEX IF NOT EXISTS fastTimestampIdx ON fastSensors(Timestamp);
            CREATE INDEX IF NOT EXISTS fastDataIdx ON fastSensors(dataIndex);
            """)
        cur.close()
        self.conn.commit()

    def _prepare_db(self, session_file: str) -> str:
        folder = self.get_db_folder()
        os.makedirs(folder, exist_ok=True)
        basename = os.path.basename(session_file)
        dbname = os.path.splitext(basename)[0] + ".db"
        return os.path.join(folder, dbname)

    def _base_date(self):
        if not self.header or self.header.year == 0:
            return datetime.utcfromtimestamp(0)
        return datetime(self.header.year + 2000, self.header.month, self.header.day, 0, 0, 0)

    def _ts_from_usec(self, usec: float) -> datetime:
        ms = int(usec / 1000.0)
        return self._base_date() + timedelta(milliseconds=ms)

    def ReadSessionFromFileV2(self, file_name: str, prog=None):
        if not os.path.exists(file_name):
            print(f"Warning: file {file_name} does not exist.")
            return

        f = open(file_name, "rb")
        data_index = 0
        start_window_time = time.time()
        prev_pos = 0
        bps = 0
        eta = 0

        if self.conn is None:
            raise ValueError("Database connection could not be established before starting transaction")
        self.conn.execute("BEGIN")

        try:
            file_size = os.path.getsize(file_name)
            
            while True:
                pos = f.tell()
                if pos >= file_size:
                    break

                peek = f.read(1)
                if not peek:
                    break
                msg_type = peek[0]
                f.seek(-1, 1)

                try:
                    match msg_type:
                        case self.MSG_SESS:  # 4 header
                            self.header = SessionHeader.parse(f)

                        case self.MSG_1KHZ:  # 13 fast sensor
                            one = SensorsData1KHZStruct.parse(f)

                            acc_scale = (self.header.acc_full_scale / 32768.0) if self.header else 1.0
                            if self.header:
                                fs = self.header.gyro_full_scale
                                gyro_scale = 0.07 if fs == 2000 else (0.035 if fs == 1000 else (7.0 / 800.0 if fs == 500 else 0.004375))
                            else:
                                gyro_scale = 1.0

                            ax, ay, az = [v * acc_scale for v in one.data.acc]
                            gx, gy, gz = [v * gyro_scale for v in one.data.gyro]
                            ts = self._ts_from_usec(one.t.msec * 100.0)
                            activation = getattr(one.data, "activation", 0)

                            resultFast = self.cur_fast.execute(
                                self.fast_insert_sql,
                                (
                                    data_index,
                                    ax, ay, az,
                                    gx, gy, gz,
                                    activation,
                                    ts.isoformat(sep=" "),
                                ),
                            )

                        case self.MSG_100HZ:  # 14 slow sensor
                            hundred = SensorsData100HZStruct.parse(f)

                            mag_scale = (self.header.mag_full_scale / 32768.0) if (self.header and self.header.mag_full_scale) else 1.0
                            ts = self._ts_from_usec(hundred.t.msec * 100.0)

                            mag_x = mag_y = mag_z = None
                            if hundred.data.mag:
                                mag_x, mag_y, mag_z = [v * mag_scale for v in hundred.data.mag]

                            rot_x = rot_y = rot_z = None
                            if hundred.data.rotation:
                                import math
                                a_deg, b_deg, g_deg = hundred.data.rotation
                                a = a_deg / 360.0 * 2.0 * math.pi
                                b = b_deg / 360.0 * 2.0 * math.pi
                                g = g_deg / 360.0 * 2.0 * math.pi
                                rot_x = math.cos(a) * math.sin(b) * math.cos(g) + math.sin(a) * math.sin(g)
                                rot_y = math.sin(a) * math.sin(b) * math.cos(g) - math.cos(a) * math.sin(g)
                                rot_z = math.cos(b) * math.cos(g)

                            grav_x = grav_y = grav_z = None
                            if hundred.data.gravity:
                                grav_x, grav_y, grav_z = hundred.data.gravity

                            activation = getattr(hundred.data, "activation", 0)

                            resultSlow = self.cur_slow.execute(
                                self.slow_insert_sql,
                                (
                                    data_index,
                                    mag_x, mag_y, mag_z,
                                    rot_x, rot_y, rot_z,
                                    grav_x, grav_y, grav_z,
                                    activation,
                                    ts.isoformat(sep=" "),
                                ),
                            )

                            if getattr(hundred.data, "latitude", 0.0) != 0.0:
                                self.cur_gps.execute(
                                    self.gps_insert_sql,
                                    (
                                        data_index,
                                        hundred.data.latitude,
                                        hundred.data.longitude,
                                        hundred.data.speed,
                                        ts.isoformat(sep=" "),
                                    ),
                                )
                                gps_data = GPSData(
                                    index=data_index,
                                    time=ts,
                                    speed=hundred.data.speed
                                )
                                gps_data.coords.Lat = hundred.data.latitude
                                gps_data.coords.Lng = hundred.data.longitude
                                gps_data.speed = hundred.data.speed
                                gps_data.time = ts
                                self.gps_data.append(gps_data)
 
                                if self.right_panel is not None and hasattr(self.right_panel, "add_path_coord"):
                                    self.right_panel.add_path_coord(hundred.data.latitude, hundred.data.longitude)
                                
                            else:
                                pass
                        case _:
                            f.read(1) 
                except Exception as e:
                    raise RuntimeError(f"Parse error at index {data_index}: {e}")
                    break
                
                now = time.time()
                if now - start_window_time > 1.0:
                    bps = int((f.tell() - prev_pos) / (now - start_window_time))
                    prev_pos = f.tell()
                    start_window_time = now
                if bps > 0:
                    eta = int((file_size - f.tell()) / bps)
                if prog:
                    prog(int(f.tell() * 100 / file_size), bps, eta)

                data_index += 1

            try:
                falls = []
                # FastSensors
                for row in self.conn.execute("SELECT dataIndex, activation FROM fastSensors WHERE activation != 0 ORDER BY dataIndex"):
                    falls.append(Fall.Fall(int(row[0]), int(row[1])))
                # SlowSensors
                for row in self.conn.execute("SELECT dataIndex, activation FROM slowSensors WHERE activation != 0 ORDER BY dataIndex"):
                    falls.append(Fall.Fall(int(row[0]), int(row[1])))
                self.falls = falls
            except Exception as e:
                print(f"Error loading falls: {e}")
                
            self.right_panel.update_timeline_range()
            self.conn.commit()
            self.MaxIndex = data_index
            try:
                if self.right_panel is not None:
                    self.right_panel.set_index_bounds(int(self.MinIndex), int(self.MaxIndex))
                    min_ts, max_ts = self.get_fast_timestamp_range()
                    self.right_panel.set_time_bounds(self._base_date(), max_ts)
            except Exception:
                pass
        except Exception as ex:
            print(f"Reading aborted: {ex}")
            self.conn.rollback()
        finally:
            f.close()

    def is_critical_fall(self, fall: int) -> bool:
            is_new = self.isNewCriticalFallFormat()
            header = fall & 0xFF000000
            if not is_new:
                return header == 0x80000000 or header == 0xF0000000
            else:
                return header == 0xFA000000

    def update_critical_falls(self):
        self.critical_falls = []
        for f in self.falls:
            if self.is_critical_fall(f.fall):
                if f not in self.critical_falls:
                    self.critical_falls.append(f)
                    
    def set_critical_falls(self):
        """
        Converte i punti di caduta in intervalli (start,end) in cui,
        secondo la logica originale di AddFallPointAnnotation, verrebbe
        disegnata una "linea rossa". Unisce indici consecutivi in un
        unico intervallo e li evidenzia con regioni rosse trasparenti.
        """
        try:
            falls = getattr(self, "falls", []) or []
            if not falls:
                # Niente da visualizzare, rimuove eventuali regioni
                self.show_sensor_activations([])
                return

            def _classify_fall(fall_val: int) -> tuple[str | None, set[int]]:
                """Calcola colore (red/green/None) e pannelli attivati in un solo passaggio."""
                try:
                    bits = int(fall_val)

                    pane_titles = [
                        self.TITLE_SPEED,
                        self.TITLE_GYRO,
                        self.TITLE_MAIN,
                        self.TITLE_GRAVITY,
                        self.TITLE_POSE,
                    ]
                    idx_gyro = pane_titles.index(self.TITLE_GYRO)
                    idx_main = pane_titles.index(self.TITLE_MAIN)
                    idx_grav = pane_titles.index(self.TITLE_GRAVITY)
                    idx_pose = pane_titles.index(self.TITLE_POSE)

                    color = None
                    triggered: set[int] = set()
                    if (bits & 0x1):
                        triggered.add(idx_main)
                    if (bits & 0x100):
                        triggered.add(idx_gyro)
                    if (bits & 0x1000):
                        triggered.add(idx_grav)
                    if (bits & 0x10000):
                        triggered.add(idx_pose)

                    # colore
                    if triggered:
                        color = "green"
                        
                    if self.is_critical_fall(bits):
                        color = "red"
                    
                    pose_flag = (bits & 0x10000) != 0
                    pose_critical_flag = (bits & 0x20000) != 0
                    if pose_flag and pose_critical_flag:
                        color = "red"

                    return color, triggered
                except Exception:
                    return None, set()

            # Ordina per indice crescente
            falls_sorted = sorted(falls, key=lambda x: int(x.index))
            intervals_by_plot_red: dict[int, list[tuple[int, int]]] = {}
            intervals_by_plot_green: dict[int, list[tuple[int, int]]] = {}
            state_red = {}
            state_green = {}
            gap_tolerance = 10

            def _update_color_state(
                idx: int,
                color: str,
                triggered_plots: set[int],
                gap_tolerance: int,
                state_dict: dict,
                intervals_dict: dict[int, list[tuple[int, int]]],
                color_name: str,
            ) -> None:
                plots_list = list(state_dict.keys() | (triggered_plots if color == color_name else set()))
                for plot_idx in plots_list:
                    st = state_dict.get(plot_idx)
                    is_trigger = (color == color_name) and (plot_idx in triggered_plots)
                    if is_trigger:
                        if st is None:
                            state_dict[plot_idx] = {"start": idx, "prev": idx, "gap": 0}
                        else:
                            # consecutivo
                            if st["prev"] is not None and idx == st["prev"] + 1:
                                st["prev"] = idx
                                st["gap"] = 0
                            else:
                                # chiudi intervallo precedente
                                intervals_dict.setdefault(plot_idx, []).append((st["start"], st["prev"]))
                                state_dict[plot_idx] = {"start": idx, "prev": idx, "gap": 0}
                    else:
                        # non trigger: se c'è intervallo aperto, applica tolleranza
                        if st is not None and st["prev"] is not None:
                            if idx == st["prev"] + 1:
                                st["gap"] += 1
                                if st["gap"] > gap_tolerance:
                                    intervals_dict.setdefault(plot_idx, []).append((st["start"], st["prev"]))
                                    state_dict.pop(plot_idx, None)
                            else:
                                # salto grande: chiudi
                                intervals_dict.setdefault(plot_idx, []).append((st["start"], st["prev"]))
                                state_dict.pop(plot_idx, None)

            for f in falls_sorted:
                idx = int(f.index)
                fall_val = int(f.fall)
                color, triggered_plots = _classify_fall(fall_val)

                # Aggiorna stato per ROSSO e VERDE con una sola funzione
                _update_color_state(idx, color, triggered_plots, gap_tolerance, state_red, intervals_by_plot_red, "red")
                _update_color_state(idx, color, triggered_plots, gap_tolerance, state_green, intervals_by_plot_green, "green")

            # Chiudi intervalli rimasti aperti
            for plot_idx, st in list(state_red.items()):
                if st and st.get("start") is not None:
                    intervals_by_plot_red.setdefault(plot_idx, []).append((st["start"], st["prev"] if st.get("prev") is not None else st["start"]))

            for plot_idx, st in list(state_green.items()):
                if st and st.get("start") is not None:
                    intervals_by_plot_green.setdefault(plot_idx, []).append((st["start"], st["prev"] if st.get("prev") is not None else st["start"]))

            def _merge_intervals(intervals: list[tuple[int,int]], max_gap: int = 5, min_width: int = 1) -> list[tuple[int,int]]:
                if not intervals:
                    return []
                intervals = sorted(intervals, key=lambda t: t[0])
                merged: list[tuple[int,int]] = []
                cs, ce = intervals[0]
                for s, e in intervals[1:]:
                    # se il gap tra ce e s è piccolo, unisci
                    if s - ce <= max_gap:
                        ce = max(ce, e)
                    else:
                        if ce - cs < min_width:
                            ce = cs + min_width
                        merged.append((cs, ce))
                        cs, ce = s, e
                if ce - cs < min_width:
                    ce = cs + min_width
                merged.append((cs, ce))
                return merged

            for pidx, ints in list(intervals_by_plot_red.items()):
                intervals_by_plot_red[pidx] = _merge_intervals(ints, max_gap=gap_tolerance, min_width=2)

            for pidx, ints in list(intervals_by_plot_green.items()):
                intervals_by_plot_green[pidx] = _merge_intervals(ints, max_gap=gap_tolerance, min_width=2)

            # Clip verde rimuovendo sovrapposizioni con rosso
            def _subtract_intervals(a: list[tuple[int,int]], b: list[tuple[int,int]]) -> list[tuple[int,int]]:
                if not a:
                    return []
                if not b:
                    return a
                b_sorted = sorted(b, key=lambda t: t[0])
                result: list[tuple[int,int]] = []
                for s, e in sorted(a, key=lambda t: t[0]):
                    segments = [(s, e)]
                    for bs, be in b_sorted:
                        new_segments = []
                        for cs, ce in segments:
                            # nessuna sovrapposizione
                            if be < cs or bs > ce:
                                new_segments.append((cs, ce))
                            else:
                                # taglia a sinistra
                                if bs > cs:
                                    new_segments.append((cs, bs - 1))
                                # taglia a destra
                                if be < ce:
                                    new_segments.append((be + 1, ce))
                        segments = [seg for seg in new_segments if seg[0] <= seg[1]]
                        if not segments:
                            break
                    result.extend(segments)
                return result

            all_plot_idxs = set(intervals_by_plot_green.keys()) | set(intervals_by_plot_red.keys())
            for pidx in all_plot_idxs:
                greens = intervals_by_plot_green.get(pidx, [])
                reds = intervals_by_plot_red.get(pidx, [])
                intervals_by_plot_green[pidx] = _subtract_intervals(greens, reds)

            self.show_colored_sensor_activations_by_plot({
                "red": intervals_by_plot_red,
                "green": intervals_by_plot_green,
            })
        except Exception as e:
            print(f"set_critical_falls error: {e}")
        
    def load_data(self, min_time: int, max_time: int):
        raise NotImplementedError

    def close_db(self):
        raise NotImplementedError

    def export_from_to(self, filename: str, min_idx: int, max_idx: int):
        raise NotImplementedError

    def CloseDB(self):
        try:
            for c in ("cur_slow", "cur_fast", "cur_gps"):
                cur = getattr(self, c, None)
                if cur is not None:
                    try:
                        cur.close()
                    except Exception:
                        pass
                    setattr(self, c, None)
            if self.conn is not None:
                try:
                    self.conn.close()
                except Exception:
                    pass
                self.conn = None
        except Exception:
            pass

    def isNewCriticalFallFormat(self) -> bool:
        return False

    def commit(self):
        try:
            if self.conn is not None:
                self.conn.commit()
        except Exception:
            pass

    def InitDB(self):
        try:
            if self.conn is None:
                dbfile = self._prepare_db(self.session_file_name)
                self.conn = sqlite3.connect(dbfile)
                self.conn.execute("PRAGMA journal_mode=WAL;")
            self._create_schema()
            self.cur_slow = self.conn.cursor()
            self.cur_fast = self.conn.cursor()
            self.cur_gps = self.conn.cursor()
        except Exception:
            pass

    def ClearData(self):
        try:
            self.MinIndex = 0
            self.MaxIndex = 0
            if self.conn is None:
                return
            self.conn.execute("BEGIN")
            self.conn.execute("DELETE FROM slowSensors;")
            self.conn.execute("DELETE FROM fastSensors;")
            self.conn.execute("DELETE FROM gpssensors;")
            self.conn.execute("DELETE FROM tags;")
            self.conn.execute("DELETE FROM notes;")
            self.conn.commit()
        except Exception:
            try:
                if self.conn is not None:
                    self.conn.rollback()
            except Exception:
                pass

    def storeSensorDataToDB(self):
        self.commit()

    def loadData(self, min_index: int, max_index: int):
        return self.load_from_to(min_index, max_index)

    def load_from_to(self, min_index: int, max_index: int):
        if self.conn is None:
            return {"fast": [], "slow": [], "gps": []}

        sampling = max(1, (max_index - min_index) // 1000)
        fast_rows = []
        slow_rows = []
        gps_rows = []

        try:
            if sampling > 100:
                fast_rows = list(self.conn.execute(
                    "SELECT dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, activation, Timestamp "
                    "FROM fastSensors WHERE dataIndex BETWEEN ? AND ? AND (rowid % ?) = 0 ORDER BY dataIndex",
                    (min_index, max_index, sampling)
                ))
                slow_rows = list(self.conn.execute(
                    "SELECT dataIndex, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, activation, Timestamp "
                    "FROM slowSensors WHERE dataIndex BETWEEN ? AND ? AND (rowid % ?) = 0 ORDER BY dataIndex",
                    (min_index, max_index, sampling // 10 if sampling // 10 > 0 else 1)
                ))
                gps_rows = list(self.conn.execute(
                    "SELECT dataIndex, latitude, longitude, speed, Timestamp FROM gpssensors "
                    "WHERE dataIndex BETWEEN ? AND ? AND (rowid % ?) = 0 ORDER BY dataIndex",
                    (min_index, max_index, sampling)
                ))
            elif sampling > 2:
                fast_rows = list(self.conn.execute(
                    "SELECT CAST(ROUND(dataIndex / ?) AS INTEGER) grp, "
                    "AVG(acc_x), AVG(acc_y), AVG(acc_z), AVG(gyro_x), AVG(gyro_y), AVG(gyro_z), MAX(activation), MIN(Timestamp) "
                    "FROM fastSensors WHERE dataIndex BETWEEN ? AND ? GROUP BY grp ORDER BY grp",
                    (sampling, min_index, max_index)
                ))
                slow_rows = list(self.conn.execute(
                    "SELECT CAST(ROUND(dataIndex / ?) AS INTEGER) grp, "
                    "AVG(mag_x), AVG(mag_y), AVG(mag_z), AVG(rot_x), AVG(rot_y), AVG(rot_z), "
                    "AVG(grav_x), AVG(grav_y), AVG(grav_z), MAX(activation), MIN(Timestamp) "
                    "FROM slowSensors WHERE dataIndex BETWEEN ? AND ? GROUP BY grp ORDER BY grp",
                    (sampling, min_index, max_index)
                ))
                gps_rows = list(self.conn.execute(
                    "SELECT CAST(ROUND(dataIndex / ?) AS INTEGER) grp, "
                    "AVG(latitude), AVG(longitude), AVG(speed), MIN(Timestamp) "
                    "FROM gpssensors WHERE dataIndex BETWEEN ? AND ? GROUP BY grp ORDER BY grp",
                    (sampling, min_index, max_index)
                ))
            else:
                fast_rows = list(self.conn.execute(
                    "SELECT dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, activation, Timestamp "
                    "FROM fastSensors WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex",
                    (min_index, max_index)
                ))
                slow_rows = list(self.conn.execute(
                    "SELECT dataIndex, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, activation, Timestamp "
                    "FROM slowSensors WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex",
                    (min_index, max_index)
                ))
                gps_rows = list(self.conn.execute(
                    "SELECT dataIndex, latitude, longitude, speed, Timestamp "
                    "FROM gpssensors WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex",
                    (min_index, max_index)
                ))
        except Exception as e:
            print(f"load_from_to error: {e}")

        return {
            "fast": fast_rows,
            "slow": slow_rows,
            "gps": gps_rows
        }
        
    def getMainData(self, sensor: int, axis: int):
        raise NotImplementedError
    
    def getGravityData(self, axis: int):
        self.graph_data.get_gravity_data(axis)
        
    def get_gravity_series(self, min_index: int | None = None, max_index: int | None = None, axis: int | None = None):
        """
        Returns (x, y) numpy arrays for gravity axis in the given range.
        """
        if self.conn is None:
            return np.array([]), np.array([])

        if axis is None:
            axis = getattr(self, "_gravity_axis", 2)

        if min_index is None:
            min_index = int(self.MinIndex)
        if max_index is None:
            max_index = int(self.MaxIndex)

        col = "grav_x" if axis == 0 else ("grav_y" if axis == 1 else "grav_z")
        sql = (
            f"SELECT dataIndex, {col} "
            "FROM slowSensors WHERE dataIndex BETWEEN ? AND ? "
            "ORDER BY dataIndex"
        )
        rows = []
        try:
            rows = list(self.conn.execute(sql, (min_index, max_index)))
        except Exception as e:
            print(f"get_gravity_series error: {e}")

        if not rows:
            return np.array([]), np.array([])

        x = np.array([r[0] for r in rows], dtype=float)
        y = np.array([r[1] if r[1] is not None else float('nan') for r in rows], dtype=float)
        return x, y
    
    def get_speed_series(self, min_index: int | None = None, max_index: int | None = None):
        if self.conn is None:
            return np.array([]), np.array([])

        if min_index is None:
            min_index = int(self.MinIndex)
        if max_index is None:
            max_index = int(self.MaxIndex)

        sql = (
            "SELECT dataIndex, speed FROM gpssensors "
            "WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex"
        )
        rows = []
        try:
            rows = list(self.conn.execute(sql, (min_index, max_index)))
        except Exception as e:
            print(f"get_speed_series error: {e}")

        if not rows:
            return np.array([]), np.array([])

        x = np.array([r[0] for r in rows], dtype=float)
        y = np.array([r[1] for r in rows], dtype=float)
        return x, y
    
    def _get_series_from_db(self, sql: str, params: tuple):
        rows = []
        try:
            rows = list(self.conn.execute(sql, params))
        except Exception as e:
            print(f"_get_series_from_db error: {e}")
        if not rows:
            return np.array([]), np.array([])
        x = np.asarray([r[0] for r in rows], dtype=float)
        y = np.asarray([r[1] for r in rows], dtype=float)
        return x, y

    def get_fast_acc_series(self, min_index: int | None = None, max_index: int | None = None, axis: int = 0):
        if self.conn is None:
            return np.array([]), np.array([])
        if min_index is None: min_index = int(self.MinIndex)
        if max_index is None: max_index = int(self.MaxIndex)
        col = "acc_x" if axis == 0 else ("acc_y" if axis == 1 else "acc_z")
        sql = f"SELECT dataIndex, {col} FROM fastSensors WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex"
        return self._get_series_from_db(sql, (min_index, max_index))

    def get_gyro_series(self, min_index: int | None = None, max_index: int | None = None, axis: int = 0):
        if self.conn is None:
            return np.array([]), np.array([])
        if min_index is None: min_index = int(self.MinIndex)
        if max_index is None: max_index = int(self.MaxIndex)
        col = "gyro_x" if axis == 0 else ("gyro_y" if axis == 1 else "gyro_z")
        sql = f"SELECT dataIndex, {col} FROM fastSensors WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex"
        return self._get_series_from_db(sql, (min_index, max_index))

    def get_pose_series(self, min_index: int | None = None, max_index: int | None = None, axis: int = 0):
        if self.conn is None:
            return np.array([]), np.array([])
        if min_index is None: min_index = int(self.MinIndex)
        if max_index is None: max_index = int(self.MaxIndex)
        col = "rot_x" if axis == 0 else ("rot_y" if axis == 1 else "rot_z")
        sql = f"SELECT dataIndex, {col} FROM slowSensors WHERE dataIndex BETWEEN ? AND ? ORDER BY dataIndex"
        return self._get_series_from_db(sql, (min_index, max_index))
    
    def get_fast_timestamp_range(self):
        """
        Restituisce il valore minimo e massimo di `Timestamp` presenti in `fastSensors`.
        Gestisce correttamente timezone/offset:
        - Se la stringa del DB ha un offset (es. "+00:00"), converte in orario locale.
        - Se è naive (senza offset), la interpreta come orario locale così com'è.
        """
        if self.conn is None:
            return None, None
        try:
            cur = self.conn.cursor()
            cur.execute(
                "SELECT MIN(Timestamp), MAX(Timestamp) FROM fastSensors WHERE Timestamp IS NOT NULL"
            )
            row = cur.fetchone()
            cur.close()
            if not row or (row[0] is None and row[1] is None):
                return None, None

            def _parse_db_ts(s: str) -> datetime | None:
                dt = datetime.fromisoformat(s)
                try:
                    return dt.replace(tzinfo=timezone.utc).astimezone()
                except Exception:
                    return dt

            min_str, max_str = row
            min_ts = _parse_db_ts(min_str)
            max_ts = _parse_db_ts(max_str)
            return min_ts, max_ts
        except Exception as e:
            print(f"get_fast_timestamp_range error: {e}")
            return None, None

    def InitSessionPlotModel(self, series, axis: int = 2):
        """
        Inizializza un layout pyqtgraph con 5 pannelli impilati (Pose, Gravity, Main, Gyro, Speed),
        assi X condivisi, serie e linee verticali per ciascun pannello.
        Aggiorna 'series' (list di liste) con i PlotDataItem corrispondenti.
        """
        LINE_WIDTH = 1.0
        CURSOR_WIDTH = 0.5
        nofgraphs = 5
        gyro_index = self.gyro_index            # 1
        speed_index = self.speed_index          # 2

        min_ts, max_ts = self.get_fast_timestamp_range()

        # Wire RightPanel time refs so labels update correctly
        try:
            rp = getattr(self, 'right_panel', None)
            if rp is not None:
                min_idx = int(getattr(self, 'MinIndex', 0))
                rp.set_session_time_refs(min_ts, min_idx)
                rp.set_time_bounds(min_ts, max_ts)
        except Exception:
            pass

        def time_formatter_from_db(v: float) -> str:
            try:
                if (
                    min_ts is not None and max_ts is not None and
                    self.MaxIndex > self.MinIndex
                ):
                    frac = (float(v) - float(self.MinIndex)) / float(self.MaxIndex - self.MinIndex)
                    if frac < 0.0:
                        frac = 0.0
                    if frac > 1.0:
                        frac = 1.0
                    ts = min_ts + (max_ts - min_ts) * frac
                    return ts.strftime("%H:%M:%S")
                try:
                    rp.set_index_bounds(int(self.MinIndex), int(self.MaxIndex))
                except Exception:
                    pass
                return str(v)
            except Exception:
                return str(v)
        
        pane_titles = [
            self.TITLE_SPEED,
            self.TITLE_GYRO,
            self.TITLE_MAIN,
            self.TITLE_GRAVITY,
            self.TITLE_POSE
        ]

        bucket_order_visual_to_series = {
            self.TITLE_SPEED: speed_index,
            self.TITLE_GYRO: gyro_index,
            self.TITLE_MAIN: 0,
            self.TITLE_POSE: 3,
            self.TITLE_GRAVITY: 4,
        }

        self.glw, self.plots, bottom_plot, _ = Graph.build_multiplot_dashboard(
            pane_titles=pane_titles,
            bottom_formatter=time_formatter_from_db,
            right_panel=self.right_panel
        )

        if not series or len(series) != nofgraphs:
            series.clear()
            series.extend([0] * nofgraphs)

        red_pen = pg.mkPen('r', width=LINE_WIDTH)
        green_pen = pg.mkPen('g', width=LINE_WIDTH)
        blue_pen = pg.mkPen(color=(80, 140, 255), width=LINE_WIDTH)
        white_pen = pg.mkPen('w', width=CURSOR_WIDTH)
        self._line_annotations = []
        vlines = Graph.add_vertical_cursors(self.plots, white_pen)
        self._line_annotations.extend(vlines)

        def update_cursor_lines(val):
            try:
                n_coords = len(self.right_panel.path_coords) if self.right_panel and hasattr(self.right_panel, "path_coords") else 1
                if n_coords > 1 and self.MaxIndex > self.MinIndex:
                    frac = val / (n_coords - 1)
                    data_idx = int(self.MinIndex + frac * (self.MaxIndex - self.MinIndex))
                else:
                    data_idx = val
                for ln in vlines:
                    ln.setValue(data_idx)
               
                for p in self.plots:
                    vb = p.getViewBox()
                    x0, x1 = vb.viewRange()[0]
                    width = x1 - x0
                    vb.setXRange(data_idx - width/2, data_idx + width/2, padding=0)
            except Exception:
                pass
        self.right_panel.timeline.valueChanged.connect(update_cursor_lines)
    
        pens3 = [red_pen, green_pen, blue_pen]
        main_bucket = bucket_order_visual_to_series[self.TITLE_MAIN]
        main_plot_idx = pane_titles.index(self.TITLE_MAIN)
        series[main_bucket] = Graph.add_curves(self.plots[main_plot_idx], 3, pens3, show_legend=True)
            
        gyro_bucket = bucket_order_visual_to_series[self.TITLE_GYRO]
        gyro_plot_idx = pane_titles.index(self.TITLE_GYRO)
        series[gyro_bucket] = Graph.add_curves(self.plots[gyro_plot_idx], 3, pens3, show_legend=True)

        speed_bucket = bucket_order_visual_to_series[self.TITLE_SPEED]
        speed_plot_idx = pane_titles.index(self.TITLE_SPEED)
        series[speed_bucket] = Graph.add_curves(self.plots[speed_plot_idx], 1, [red_pen], show_legend=True)

        pose_bucket = bucket_order_visual_to_series[self.TITLE_POSE]
        pose_plot_idx = pane_titles.index(self.TITLE_POSE)
        series[pose_bucket] = Graph.add_curves(self.plots[pose_plot_idx], 3, pens3, show_legend=True)

        grav_bucket = bucket_order_visual_to_series[self.TITLE_GRAVITY]
        grav_plot_idx = pane_titles.index(self.TITLE_GRAVITY)
        series[grav_bucket] = Graph.add_curves(self.plots[grav_plot_idx], 3, pens3, show_legend=True)

        Graph.connect_lod(bottom_plot, target_pts=2000)
        self._plots = self.plots
        self._activation_regions = []

    def apply_y_ticks(self):
        try:
            plots = getattr(self, "_plots", [])
            acc_plots = getattr(self, "_acc_plots", set())
            for p in plots:
                if p in acc_plots:
                    p.getAxis('left').setTicks([[(0.0, "0")]])
                    continue
                ys_max = 0.0
                for it in p.listDataItems():
                    xd, yd = it.getData()
                    if yd is None:
                        continue
                    try:
                        m = float(np.nanmax(np.abs(yd)))
                        if m > ys_max:
                            ys_max = m
                    except Exception:
                        pass
                if ys_max <= 0:
                    continue
                steps = [-1, 0.0, 1]
                vals = [s * ys_max for s in steps]
                def fmt(v):
                    av = abs(ys_max)
                    if av >= 10:
                        return f"{int(round(v))}"
                    return f"{v:.2f}"
                ticks = [[(v, fmt(v)) for v in vals]]
                p.getAxis('left').setTicks(ticks)
        except Exception:
            pass

    def show_sensor_activations(self, intervals: list[tuple[int, int]]):
        """
        Disegna rettangoli rossi trasparenti su tutti i grafici per ogni intervallo (start_index, end_index).
        """
        try:
            plots = getattr(self, "_plots", [])
            if not plots:
                return
            # Rimuove regioni esistenti
            try:
                for plot_regions in getattr(self, "_activation_regions", []):
                    for reg in plot_regions:
                        try:
                            plots[0].scene().removeItem(reg)
                        except Exception:
                            pass
            except Exception:
                pass
            # Aggiunge nuove regioni
            self._activation_regions = Graph.add_time_regions(plots, intervals, color=(255, 0, 0, 80))
        except Exception:
            pass

    def show_sensor_activations_by_plot(self, intervals_by_plot: dict[int, list[tuple[int, int]]]):
        """
        Disegna rettangoli rossi trasparenti solo sui grafici specificati da intervals_by_plot.
        intervals_by_plot: dict plot_index -> list[(start_index, end_index)]
        """
        try:
            plots = getattr(self, "_plots", [])
            if not plots:
                return
            # Rimuovi regioni esistenti
            try:
                for plot_regions in getattr(self, "_activation_regions", []):
                    for reg in plot_regions:
                        try:
                            plots[0].scene().removeItem(reg)
                        except Exception:
                            pass
            except Exception:
                pass
            self._activation_regions = []
            # Aggiungi regioni per ciascun plot
            for plot_idx, intervals in intervals_by_plot.items():
                if plot_idx < 0 or plot_idx >= len(plots):
                    continue
                regs = Graph.add_time_regions([plots[plot_idx]], intervals, color=(255, 0, 0, 80))
                self._activation_regions.append(regs)
        except Exception:
            pass

    def show_colored_sensor_activations_by_plot(self, intervals_by_color: dict[str, dict[int, list[tuple[int, int]]]]):
        """Disegna regioni per colore e pannello."""
        try:
            plots = getattr(self, "_plots", [])
            if not plots:
                return
            try:
                for plot_regions in getattr(self, "_activation_regions", []):
                    for reg in plot_regions:
                        try:
                            plots[0].scene().removeItem(reg)
                        except Exception:
                            pass
            except Exception:
                pass
            self._activation_regions = []
            color_map = {
                "red": (255, 0, 0, 80),
                "green": (0, 255, 0, 80),
                "yellow": (255, 255, 0, 80),
            }
            for color_key, intervals_by_plot in intervals_by_color.items():
                col = color_map.get(color_key, (255, 0, 0, 80))
                for plot_idx, intervals in intervals_by_plot.items():
                    if plot_idx < 0 or plot_idx >= len(plots):
                        continue
                    regs = Graph.add_time_regions([plots[plot_idx]], intervals, color=col)
                    self._activation_regions.append(regs)
        except Exception:
            pass


