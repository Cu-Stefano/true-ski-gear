import os
import sqlite3
import struct
import time
from datetime import datetime, timedelta
from dataclasses import dataclass
from BaseSession import BaseSession

from session_header import SessionHeader
from sensors_data_1khz import SensorsData1KHZStruct
from sensors_data_100hz import SensorsData100HZStruct
from data_header import DataHeader

class SessionV2(BaseSession):

    MSG_SESS = 4
    MSG_1KHZ = 13
    MSG_100HZ = 14

    MAX_GRAPHS = 9
    MAX_SENSORS = 5

    gyro_index = 5
    speed_index = 6
    
    def __init__(self, device_id: int, session_id: int, filename: str, right_panel=None):
        super().__init__(nofgraphs=9, nofsensors=4)
        self.session_version = 2
        self.right_panel = right_panel
        self.device_id = device_id
        self.session_id = session_id
        self.session_file_name = filename
        self.header: SessionHeader | None = None

        dbfile = self._prepare_db(filename)
        self.conn = sqlite3.connect(dbfile)
        self.conn.execute("PRAGMA journal_mode=WAL;")

        # ------------------------------------------------------------------
        # Create tables and indexes EXACTLY matching C# (names & attributes)
        # ------------------------------------------------------------------
        self._create_schema()

        # Prepare reusable insert statements (matching C# column order)
        self.fast_insert_sql = (
            "INSERT INTO fastSensors "
            "(dataIndex, acc_x, acc_y, acc_z, gyro_x, gyro_y, gyro_z, activation, Timestamp) "
            "VALUES (?,?,?,?,?,?,?,?,?)"
        )
        self.slow_insert_sql = (
            "INSERT INTO slowSensors "
            "(dataIndex, acc_0_x, acc_0_y, acc_0_z, "
            " acc_1_x, acc_1_y, acc_1_z, "
            " acc_2_x, acc_2_y, acc_2_z, "
            " acc_3_x, acc_3_y, acc_3_z, "
            " mag_x, mag_y, mag_z, "
            " rot_x, rot_y, rot_z, "
            " grav_x, grav_y, grav_z, activation, Timestamp) "
            "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"
        )
        self.gps_insert_sql = (
            "INSERT INTO gpssensors (dataIndex, latitude, longitude, speed, Timestamp) "
            "VALUES (?,?,?,?,?)"
        )

        # Cursors for bulk insertion
        self.cur_fast = self.conn.cursor()
        self.cur_slow = self.conn.cursor()
        self.cur_gps = self.conn.cursor()

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
                acc_0_x,
                acc_0_y,
                acc_0_z,
                acc_1_x,
                acc_1_y,
                acc_1_z,
                acc_2_x,
                acc_2_y,
                acc_2_z,
                acc_3_x,
                acc_3_y,
                acc_3_z,
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

    # ---------------- Timestamp logic (mirrors C# GenerateSessionTimestampFromUSec) ----------
    def _base_date(self):
        if not self.header or self.header.year == 0:
            return datetime.utcfromtimestamp(0)
        return datetime(self.header.year + 2000, self.header.month, self.header.day, 0, 0, 0)

    def _ts_from_usec(self, usec: float) -> datetime:
        ms = int(usec / 1000.0)
        return self._base_date() + timedelta(milliseconds=ms)

    # ---------------- Core reading method ----------------------------------------------------
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
                    if msg_type == self.MSG_SESS:
                        raw = f.read(40)
                        if len(raw) < 40:
                            break
                        self.header = SessionHeader.parse(raw)

                    elif msg_type == self.MSG_1KHZ:
                        raw = f.read(22)
                        if len(raw) < 22:
                            break
                        one = SensorsData1KHZStruct.parse(raw)
                        acc_scale = self.header.acc_full_scale / 32768.0 if self.header else 1.0
                        if self.header:
                            fs = self.header.gyro_full_scale
                            gyro_scale = 0.07 if fs == 2000 else (0.035 if fs == 1000 else (7.0/800.0 if fs == 500 else 0.004375))
                        else:
                            gyro_scale = 1.0
                        ax, ay, az = [v * acc_scale for v in one.data.acc]
                        gx, gy, gz = [v * gyro_scale for v in one.data.gyro]
                        ts = self._ts_from_usec(one.t.msec * 100.0)
                        activation = getattr(one.data, "activation", 0)

                        self.cur_fast.execute(
                            self.fast_insert_sql,
                            (
                                data_index,
                                ax, ay, az,
                                gx, gy, gz,
                                activation,
                                ts.isoformat(sep=' ')
                            )
                        )

                    elif msg_type == self.MSG_100HZ:
                        raw_header = f.read(2)
                        if len(raw_header) < 2:
                            break
                        dh = DataHeader.parse(raw_header)
                        f.seek(-2, 1)
                        # Minimum legacy size 74, else dh.size
                        size_to_read = max(74, dh.size)
                        raw = f.read(size_to_read)
                        if len(raw) < 74:
                            break
                        hundred = SensorsData100HZStruct.parse(raw)

                        acc_scale = self.header.acc_full_scale / 32768.0 if self.header else 1.0
                        mag_scale = self.header.mag_full_scale / 32768.0 if (self.header and self.header.mag_full_scale) else 1.0
                        ts = self._ts_from_usec(hundred.t.msec * 100.0)

                        acc_vals = hundred.data.acc  # 12 values (4 * 3)
                        acc0 = [acc_vals[0] * acc_scale, acc_vals[1] * acc_scale, acc_vals[2] * acc_scale]
                        acc1 = [acc_vals[3] * acc_scale, acc_vals[4] * acc_scale, acc_vals[5] * acc_scale]
                        acc2 = [acc_vals[6] * acc_scale, acc_vals[7] * acc_scale, acc_vals[8] * acc_scale]
                        acc3 = [acc_vals[9] * acc_scale, acc_vals[10] * acc_scale, acc_vals[11] * acc_scale]

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

                        self.cur_slow.execute(
                            self.slow_insert_sql,
                            (
                                data_index,
                                *acc0, *acc1, *acc2, *acc3,
                                mag_x, mag_y, mag_z,
                                rot_x, rot_y, rot_z,
                                grav_x, grav_y, grav_z,
                                activation,
                                ts.isoformat(sep=' ')
                            )
                        )

                        # GPS (only if latitude != 0 like C#)
                        if hundred.data.latitude != 0.0:
                            self.cur_gps.execute(
                                self.gps_insert_sql,
                                (
                                    data_index,
                                    hundred.data.latitude,
                                    hundred.data.longitude,
                                    hundred.data.speed,
                                    ts.isoformat(sep=' ')
                                )
                            )
                        else:
                            # Still log warning style (optional)
                            pass
                    else:
                        f.read(1)  # consume unsupported type

                except Exception as e:
                    print(f"Parse error at index {data_index}: {e}")
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
                    "SELECT dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, "
                    "acc_3_x, acc_3_y, acc_3_z, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, activation, Timestamp "
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
                    "AVG(acc_0_x), AVG(acc_0_y), AVG(acc_0_z), AVG(acc_1_x), AVG(acc_1_y), AVG(acc_1_z), "
                    "AVG(acc_2_x), AVG(acc_2_y), AVG(acc_2_z), AVG(acc_3_x), AVG(acc_3_y), AVG(acc_3_z), "
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
                    "SELECT dataIndex, acc_0_x, acc_0_y, acc_0_z, acc_1_x, acc_1_y, acc_1_z, acc_2_x, acc_2_y, acc_2_z, "
                    "acc_3_x, acc_3_y, acc_3_z, mag_x, mag_y, mag_z, rot_x, rot_y, rot_z, grav_x, grav_y, grav_z, activation, Timestamp "
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