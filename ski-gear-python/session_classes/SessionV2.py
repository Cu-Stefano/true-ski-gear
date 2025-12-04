import os
import sqlite3
import time
from datetime import datetime, timedelta
from dataclasses import dataclass
from .BaseSession import BaseSession
import numpy as np
from data_classes.session_header import SessionHeader
from data_classes.GPSData import GPSData
from data_classes.sensors_data_1khzStruct import SensorsData1KHZStruct
from data_classes.sensors_data_100hzStruct import SensorsData100HZStruct
import pyqtgraph as pg
from Graph import NoRightZoomViewBox

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

                            # Accelerometri 1..4 non utilizzati: ignorati

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

            self.right_panel.update_timeline_range()
            self.conn.commit()
            self.MaxIndex = data_index
        except Exception as ex:
            print(f"Reading aborted: {ex}")
            self.conn.rollback()
        finally:
            f.close()

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
    
    def get_slow_timestamp_range(self):
     """
     Restituisce il valore minimo e massimo di `Timestamp` presenti in `slowSensors`.
     Ritorna:
     - (min_ts, max_ts) come oggetti `datetime` se disponibili
     - (None, None) se la tabella è vuota o i timestamp mancano
     """
     if self.conn is None:
         return None, None
     try:
         cur = self.conn.cursor()
         cur.execute(
             "SELECT MIN(Timestamp), MAX(Timestamp) FROM slowSensors WHERE Timestamp IS NOT NULL"
         )
         row = cur.fetchone()
         cur.close()
         if not row or (row[0] is None and row[1] is None):
             return None, None
         min_str, max_str = row
         min_ts = datetime.fromisoformat(min_str) if min_str else None
         max_ts = datetime.fromisoformat(max_str) if max_str else None
         return min_ts, max_ts
     except Exception as e:
         print(f"get_slow_timestamp_range error: {e}")
         return None, None

    def InitSessionPlotModel(self, series, axis: int = 2):
        """
        Inizializza un layout pyqtgraph con 5 pannelli impilati (Pose, Gravity, Main, Gyro, Speed),
        assi X condivisi, serie e linee verticali per ciascun pannello.
        Aggiorna 'series' (list di liste) con i PlotDataItem corrispondenti.
        """
        LINE_WIDTH = 1.0
        CURSOR_WIDTH = 1.0
        nofsensors = 5
        nofgraphs = 5
        gyro_index = self.gyro_index            # 1
        speed_index = self.speed_index          # 2

        min_ts, max_ts = self.get_slow_timestamp_range()

        def time_formatter_from_db(v: float) -> str:
            try:
                if min_ts is not None and max_ts is not None and self.MaxIndex > self.MinIndex:
                    # Interpolazione lineare tra dataIndex e timestamp
                    frac = (v - self.MinIndex) / (self.MaxIndex - self.MinIndex)
                    ts = min_ts + (max_ts - min_ts) * frac
                    h = ts.hour
                    m = ts.minute
                    s = ts.second
                    return f"{h:02d}:{m:02d}:{s:02d}"
                else:
                    return str(v)
            except Exception:
                return str(v)

        class TimeAxis(pg.AxisItem):
            def __init__(self, *args, **kwargs):
                self._formatter = kwargs.pop("formatter", None)
                super().__init__(*args, **kwargs)
            def tickStrings(self, values, scale, spacing):
                if self._formatter:
                    return [self._formatter(v) for v in values]
                return super().tickStrings(values, scale, spacing)

        pg.setConfigOptions(antialias=True)
        glw = pg.GraphicsLayoutWidget(show=False)
        glw.ci.setContentsMargins(0, 0, 0, 0)
        glw.ci.layout.setSpacing(0) 
        try:
            glw.ci.setBorder(None)
        except Exception:
            pass

        if self.right_panel is not None:
            try:
                layout = self.right_panel.layout()
                if hasattr(self.right_panel, "graph_frame") and self.right_panel.graph_frame is not None:
                    old = self.right_panel.graph_frame
                    layout.replaceWidget(old, glw)
                    old.deleteLater()
                else:
                    layout.insertWidget(0, glw)
                self.right_panel.graph_frame = glw
            except Exception:
                pass

        comp_colors = ['r', 'g', 'b']
        
        pane_titles = [
            "Speed",
            "Gyro",
            "Main",
            "Gravity",
            "Pose"
        ]

        bucket_order_visual_to_series = {
            "Speed":       speed_index,
            "Gyro":        gyro_index,
            "Main":        0,
            "Pose":        3,
            "Gravity":     4,
        }
        plots = []
        vlines = []
        
        bottom_axis = TimeAxis(orientation='bottom', formatter=time_formatter_from_db)
        self._acc_plots = set()
        for row, title in enumerate(pane_titles):
            vb = NoRightZoomViewBox()
            vb.setDefaultPadding(0.0)
            if title == pane_titles[-1]:
                p = pg.PlotItem(viewBox=vb, axisItems={'bottom': bottom_axis})
            else:
                p = pg.PlotItem(viewBox=vb)

            try:
                p.layout.setContentsMargins(0, 0, 0, 0)
                p.setDefaultPadding(0.0)
            except Exception:
                pass

            p.setLabel('left', title, color='w')
            p.getAxis('left').setTextPen('w')
            p.getAxis('left').setPen('w')
            try:
                p.getAxis('left').setWidth(50)
            except Exception:
                pass

            if title != pane_titles[-1]:
                p.setLimits(xMin=0.0, minXRange=300.0, maxXRange=300000.0)
                p.getAxis('bottom').setStyle(showValues=False)
                p.getAxis('bottom').setHeight(0)
            else:
                p.setLimits(xMin=0.0, minXRange=300.0, maxXRange=300000.0)
                p.getAxis('bottom').setTextPen('w')
                p.getAxis('bottom').setPen('w')

            if title == "Speed":
                p.showGrid(x=False, y=False, alpha=0.3)
            else:
                p.showGrid(x=False, y=False, alpha=0.3)

            vb.setMouseEnabled(x=True, y=False)

            glw.addItem(p)
            if row < len(pane_titles) - 1:
                glw.nextRow()

            plots.append(p)

        bottom_plot = plots[-1]
        bottom_plot.getViewBox().setDefaultPadding(0.0)
        for p in plots[:-1]:
            p.getViewBox().setDefaultPadding(0.0)
            p.setXLink(bottom_plot)

        if not series or len(series) != nofgraphs:
            series.clear()
            series.extend([0] * nofgraphs)

        red_pen = pg.mkPen('r', width=LINE_WIDTH)
        green_pen = pg.mkPen('g', width=LINE_WIDTH)
        blue_pen = pg.mkPen(color=(80, 140, 255), width=LINE_WIDTH)
        white_pen = pg.mkPen('w', width=CURSOR_WIDTH)
        self._line_annotations = []
        vlines = []

        white_pen = pg.mkPen('w', width=CURSOR_WIDTH)
        for p in plots:
            ln = pg.InfiniteLine(angle=90, movable=False, pen=white_pen)
            p.addItem(ln)
            vlines.append(ln)
            self._line_annotations.append(ln)

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
               
                for p in plots:
                    vb = p.getViewBox()
                    x0, x1 = vb.viewRange()[0]
                    width = x1 - x0
                    vb.setXRange(data_idx - width/2, data_idx + width/2, padding=0)
            except Exception:
                pass
   
        self.right_panel.timeline.valueChanged.connect(update_cursor_lines)
    
        def add_curves_to_plot(plot_item: pg.PlotItem, count: int, pens: list[pg.QtGui.QPen]):
            items = []
            for i in range(count):
                it = pg.PlotDataItem(pen=pens[i])
                try:
                    it.setClipToView(True)
                    it.setDownsampling(1, True, mode='subsample')
                    it.setAutoDownsample(True)
                except Exception:
                    pass
                plot_item.addItem(it)
                items.append(it)
            return items

        pens3 = [red_pen, green_pen, blue_pen]
        main_bucket = bucket_order_visual_to_series["Main"]
        main_plot_idx = pane_titles.index("Main")
        series[main_bucket] = add_curves_to_plot(plots[main_plot_idx], 3, pens3)
            
        gyro_bucket = bucket_order_visual_to_series["Gyro"]
        gyro_plot_idx = pane_titles.index("Gyro")
        series[gyro_bucket] = add_curves_to_plot(plots[gyro_plot_idx], 3, pens3)

        speed_bucket = bucket_order_visual_to_series["Speed"]
        speed_plot_idx = pane_titles.index("Speed")
        series[speed_bucket] = add_curves_to_plot(plots[speed_plot_idx], 1, [red_pen])

        pose_bucket = bucket_order_visual_to_series["Pose"]
        pose_plot_idx = pane_titles.index("Pose")
        series[pose_bucket] = add_curves_to_plot(plots[pose_plot_idx], 3, pens3)

        grav_bucket = bucket_order_visual_to_series["Gravity"]
        grav_plot_idx = pane_titles.index("Gravity")
        series[grav_bucket] = add_curves_to_plot(plots[grav_plot_idx], 3, pens3)

        def _apply_lod():
            try:
                vb = bottom_plot.getViewBox()
                x0, x1 = vb.viewRange()[0]
                target_pts = 2000
                step = max(1, int((x1 - x0) / max(1.0, target_pts)))
            except Exception:
                pass

        try:
            bottom_plot.getViewBox().sigXRangeChanged.connect(lambda *_: _apply_lod())
        except Exception:
            pass


        self._plots = plots

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


