from __future__ import annotations

import collections
import datetime as dt
import logging
import math
import sqlite3
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, BinaryIO, Optional


# ---------------------------------------------------------------------------
# Data structures (adattale alle tue classi reali)
# ---------------------------------------------------------------------------

@dataclass
class SessionHeader:
    acc_full_scale: int
    gyro_full_scale: int
    mag_full_scale: int

    @classmethod
    def from_file(cls, f: BinaryIO) -> "SessionHeader":
        # TODO: implementa il parsing binario del tuo header
        raise NotImplementedError


@dataclass
class SensorsData1KHZStruct:
    # qui metto solo i campi che vengono usati nel metodo
    class Data:
        def __init__(self, acc, gyro, activation):
            self.acc = acc
            self.gyro = gyro
            self.activation = activation

    class T:
        def __init__(self, msec):
            self.msec = msec

    data: Data
    t: T

    @classmethod
    def from_file(cls, f: BinaryIO) -> "SensorsData1KHZStruct":
        # TODO: implementa il parsing binario della struct 1kHz
        raise NotImplementedError


@dataclass
class SensorsData100HZStruct:
    class Header:
        def __init__(self, size: int):
            self.size = size

    class Data:
        def __init__(self, acc, mag, rotation, gravity,
                     activation, latitude, longitude, speed):
            self.acc = acc
            self.mag = mag
            self.rotation = rotation
            self.gravity = gravity
            self.activation = activation
            self.latitude = latitude
            self.longitude = longitude
            self.speed = speed

    class T:
        def __init__(self, msec):
            self.msec = msec

    header: Header
    data: Data
    t: T

    @classmethod
    def from_file(cls, f: BinaryIO) -> "SensorsData100HZStruct":
        # TODO: implementa il parsing binario della struct 100Hz
        raise NotImplementedError


@dataclass
class GPSCoords:
    lat: float
    lng: float


@dataclass
class GPSData:
    data_index: int
    time: dt.datetime
    speed: float
    coords: GPSCoords


@dataclass
class Fall:
    data_index: int
    activation: int


@dataclass
class ReadSessionResult:
    max_index: int
    falls: list[Fall]
    gps_data: list[GPSData]
    min_time: Optional[dt.datetime]
    max_time: Optional[dt.datetime]
    session_header: Optional[SessionHeader]


class FFTMotDetect:
    """Stub, sostituisci con la tua implementazione reale."""
    def add_sample(self, value: float) -> int:
        # TODO: logica di motion detection
        return 0


def generate_session_timestamp_from_usec(usec: float) -> dt.datetime:
    """
    Versione placeholder di GenerateSessionTimestampFromUSec.
    Adattala alla tua logica (epoch di partenza, ecc.).
    """
    epoch = dt.datetime(1970, 1, 1, tzinfo=dt.timezone.utc)
    return epoch + dt.timedelta(microseconds=usec)


def _get_timezone_offset_hours_from_db_bounds(
    conn: sqlite3.Connection,
    logger: logging.Logger,
) -> Optional[float]:
    """
    Stima l'offset orario (in ore) dal bounding box di lat/lon in tabella gpssensors.
    Usa timezonefinder + zoneinfo se disponibili, altrimenti restituisce None.
    """
    cur = conn.execute(
        "SELECT MIN(latitude), MAX(latitude), MIN(longitude), MAX(longitude) "
        "FROM gpssensors"
    )
    row = cur.fetchone()
    if not row or any(val is None for val in row):
        return None

    min_lat, max_lat, min_lon, max_lon = row
    lat = (min_lat + max_lat) / 2.0
    lon = (min_lon + max_lon) / 2.0

    try:
        from timezonefinder import TimezoneFinder  # type: ignore
        from zoneinfo import ZoneInfo  # Python 3.9+

        tf = TimezoneFinder()
        tzname = tf.timezone_at(lat=lat, lng=lon)
        if not tzname:
            return None

        tzinfo = ZoneInfo(tzname)
        now_utc = dt.datetime.now(dt.timezone.utc)
        offset = now_utc.astimezone(tzinfo).utcoffset()
        if offset is None:
            return None
        return offset.total_seconds() / 3600.0
    except Exception as exc:  # dipende da lib esterne
        logger.warning("Impossibile determinare timezone da coordinate GPS: %s", exc)
        return None


ReadSessionProgress = Callable[[int, int, int], None]


def read_session_from_file_v2(
    file_name: str,
    conn: sqlite3.Connection,
    *,
    progress_cb: Optional[ReadSessionProgress] = None,
    logger: Optional[logging.Logger] = None,
    batch_size: int = 10_000,
) -> ReadSessionResult:
    """
    Porting Python "pulito" di ReadSessionFromFileV2.

    - file_name: percorso del file .dat
    - conn: connessione sqlite3 già aperta
    - progress_cb: callback opzionale (percentuale, bytes/sec, eta_sec)
    - batch_size: ogni quanti record fare commit esplicito
    """
    logger = logger or logging.getLogger(__name__)

    if not Path(file_name).is_file():
        logger.warning("File %s non esiste", file_name)
        return ReadSessionResult(
            max_index=0,
            falls=[],
            gps_data=[],
            min_time=None,
            max_time=None,
            session_header=None,
        )

    session_header: Optional[SessionHeader] = None
    gps_data: list[GPSData] = []
    falls: list[Fall] = []

    data_index = 0
    my_q: "collections.deque[tuple[float, float, float]]" = collections.deque()
    mot_det = FFTMotDetect()
    db_num = 0  # inutile come nel codice originale, ma lo mantengo

    with open(file_name, "rb") as f:
        # calcolo dimensione file
        f.seek(0, 2)
        file_size = f.tell()
        f.seek(0)

        # cursori
        cur_fast = conn.cursor()
        cur_slow = conn.cursor()
        cur_gps = conn.cursor()

        def begin_tx() -> None:
            conn.execute("BEGIN")

        begin_tx()
        start_time = dt.datetime.now(dt.timezone.utc)
        prev_pos = 0
        bps = 0
        eta = 0

        try:
            while f.tell() < file_size:
                # "Peek" del prossimo byte come in PeekChar
                pos = f.tell()
                raw_type = f.read(1)
                if not raw_type:
                    break
                record_type = raw_type[0]
                f.seek(pos)

                # switch(type)
                if record_type == 4:
                    # SessionHeader
                    try:
                        session_header = SessionHeader.from_file(f)
                    except Exception as exc:
                        logger.warning("Errore nel leggere SessionHeader: %s", exc)
                        # come nel C#: salta alla fine del file
                        f.seek(file_size)
                        continue

                elif record_type == 13:
                    # SensorsData1KHZStruct
                    try:
                        s1k = SensorsData1KHZStruct.from_file(f)
                    except Exception as exc:
                        logger.warning("Errore nel leggere SensorsData1KHZStruct: %s", exc)
                        f.seek(file_size)
                        continue

                    if session_header is None:
                        logger.error(
                            "SessionHeader non letto prima dei dati fast (type=13); "
                            "interrompo."
                        )
                        break

                    acc_scale = session_header.acc_full_scale / (2 ** 16 / 2.0)
                    gyro_fs = session_header.gyro_full_scale
                    if gyro_fs == 2000:
                        gyro_scale = 0.07
                    elif gyro_fs == 1000:
                        gyro_scale = 0.035
                    elif gyro_fs == 500:
                        gyro_scale = 7.0 / 800.0
                    else:
                        gyro_scale = 0.004375

                    try:
                        db_num_ret = mot_det.add_sample(
                            float(s1k.data.acc[2]) * 0.244 * 0.001
                        )
                        if db_num_ret:
                            db_num = db_num_ret  # solo per mantenere semantica

                        params = {
                            "dataIndex": data_index,
                            "acc_x": float(s1k.data.acc[0]) * acc_scale,
                            "acc_y": float(s1k.data.acc[1]) * acc_scale,
                            "acc_z": float(s1k.data.acc[2]) * acc_scale,
                            "gyro_x": float(s1k.data.gyro[0]) * gyro_scale,
                            "gyro_y": float(s1k.data.gyro[1]) * gyro_scale,
                            "gyro_z": float(s1k.data.gyro[2]) * gyro_scale,
                            "activation": int(s1k.data.activation),
                            "time": generate_session_timestamp_from_usec(
                                float(s1k.t.msec) * 100.0
                            ),
                        }

                        cur_fast.execute(
                            """
                            INSERT INTO fastSensors (
                                dataIndex,
                                acc_x, acc_y, acc_z,
                                gyro_x, gyro_y, gyro_z,
                                activation,
                                Timestamp
                            ) VALUES (
                                :dataIndex,
                                :acc_x, :acc_y, :acc_z,
                                :gyro_x, :gyro_y, :gyro_z,
                                :activation,
                                :time
                            )
                            """,
                            params,
                        )
                    except Exception as exc:
                        logger.error(
                            "Impossibile salvare fast sensor data a indice %d: %s",
                            data_index,
                            exc,
                        )

                elif record_type == 14:
                    # SensorsData100HZStruct
                    try:
                        s100 = SensorsData100HZStruct.from_file(f)
                    except Exception as exc:
                        logger.warning("Errore nel leggere SensorsData100HZStruct: %s", exc)
                        f.seek(file_size)
                        continue

                    try:
                        if s100.header.size >= 74:
                            if session_header is None:
                                logger.error(
                                    "SessionHeader non letto prima dei dati slow (type=14); "
                                    "interrompo."
                                )
                                break

                            acc_scale = session_header.acc_full_scale / (2 ** 16 / 2.0)
                            mag_scale = session_header.mag_full_scale / (2 ** 16 / 2.0)

                            # Rotazioni
                            g = float(s100.data.rotation[2]) / 360.0 * 2.0 * math.pi
                            b = float(s100.data.rotation[1]) / 360.0 * 2.0 * math.pi
                            a = float(s100.data.rotation[0]) / 360.0 * 2.0 * math.pi
                            x = (math.cos(a) * math.sin(b) * math.cos(g) +
                                 math.sin(a) * math.sin(g))
                            y = (math.sin(a) * math.sin(b) * math.cos(g) -
                                 math.cos(a) * math.sin(g))
                            z = math.cos(b) * math.cos(g)
                            my_q.append((x, y, z))

                            params = {"dataIndex": data_index}
                            # 4 campioni accelerometro
                            for i in range(4):
                                base_idx = i * 3
                                params[f"acc_{i}_x"] = int(float(s100.data.acc[base_idx]) * acc_scale)
                                params[f"acc_{i}_y"] = int(
                                    float(s100.data.acc[base_idx + 1]) * acc_scale
                                )
                                params[f"acc_{i}_z"] = int(
                                    float(s100.data.acc[base_idx + 2]) * acc_scale
                                )

                            params.update(
                                mag_x=int(float(s100.data.mag[0]) * mag_scale),
                                mag_y=int(float(s100.data.mag[1]) * mag_scale),
                                mag_z=int(float(s100.data.mag[2]) * mag_scale),
                                rot_x=int(x),
                                rot_y=int(y),
                                rot_z=int(z),
                                grav_x=int(float(s100.data.gravity[0])),
                                grav_y=int(float(s100.data.gravity[1])),
                                grav_z=int(float(s100.data.gravity[2])),
                                activation=int(s100.data.activation),
                                time=int(generate_session_timestamp_from_usec(
                                    float(s100.t.msec) * 100.0
                                ).timestamp()),
                            )

                            cur_slow.execute(
                                """
                                INSERT INTO slowSensors (
                                    dataIndex,
                                    acc_0_x, acc_0_y, acc_0_z,
                                    acc_1_x, acc_1_y, acc_1_z,
                                    acc_2_x, acc_2_y, acc_2_z,
                                    acc_3_x, acc_3_y, acc_3_z,
                                    mag_x, mag_y, mag_z,
                                    rot_x, rot_y, rot_z,
                                    grav_x, grav_y, grav_z,
                                    activation,
                                    Timestamp
                                ) VALUES (
                                    :dataIndex,
                                    :acc_0_x, :acc_0_y, :acc_0_z,
                                    :acc_1_x, :acc_1_y, :acc_1_z,
                                    :acc_2_x, :acc_2_y, :acc_2_z,
                                    :acc_3_x, :acc_3_y, :acc_3_z,
                                    :mag_x, :mag_y, :mag_z,
                                    :rot_x, :rot_y, :rot_z,
                                    :grav_x, :grav_y, :grav_z,
                                    :activation,
                                    :time
                                )
                                """,
                                params,
                            )

                            # GPS completo
                            if float(s100.data.latitude) != 0.0:
                                time = generate_session_timestamp_from_usec(
                                    float(s100.t.msec) * 100.0
                                )
                                gps_params = {
                                    "dataIndex": data_index,
                                    "latitude": float(s100.data.latitude),
                                    "longitude": float(s100.data.longitude),
                                    "speed": float(s100.data.speed),
                                    "time": time,
                                }
                                cur_gps.execute(
                                    """
                                    INSERT INTO gpssensors (
                                        dataIndex, latitude, longitude, speed, Timestamp
                                    ) VALUES (
                                        :dataIndex, :latitude, :longitude, :speed, :time
                                    )
                                    """,
                                    gps_params,
                                )
                                gps_data.append(
                                    GPSData(
                                        data_index=data_index,
                                        time=time,
                                        speed=float(s100.data.speed),
                                        coords=GPSCoords(
                                            lat=float(s100.data.latitude),
                                            lng=float(s100.data.longitude),
                                        ),
                                    )
                                )
                            else:
                                logger.warning("Nessun dato di longitudine (latitude == 0)")

                        else:
                            # versione vecchia: solo GPS
                            time = generate_session_timestamp_from_usec(
                                float(s100.t.msec) * 100.0
                            )
                            gps_params = {
                                "dataIndex": data_index,
                                "latitude": float(s100.data.latitude),
                                "longitude": float(s100.data.longitude),
                                "speed": float(s100.data.speed),
                                "time": time,
                            }
                            cur_gps.execute(
                                """
                                INSERT INTO gpssensors (
                                    dataIndex, latitude, longitude, speed, Timestamp
                                ) VALUES (
                                    :dataIndex, :latitude, :longitude, :speed, :time
                                )
                                """,
                                gps_params,
                            )
                            gps_data.append(
                                GPSData(
                                    data_index=data_index,
                                    time=time,
                                    speed=float(s100.data.speed),
                                    coords=GPSCoords(
                                        lat=float(s100.data.latitude),
                                        lng=float(s100.data.longitude),
                                    ),
                                )
                            )
                    except Exception as exc:
                        logger.error(
                            "Impossibile salvare slow/gps sensor data a indice %d: %s",
                            data_index,
                            exc,
                        )

                else:
                    logger.warning("Tipo di dato non supportato: %d", record_type)
                    # salta un byte come nel reader.ReadChar()
                    f.read(1)

                # Aggiorno statistiche bps / eta
                now = dt.datetime.now(dt.timezone.utc)
                if (now - start_time).total_seconds() > 1.0:
                    cur_pos = f.tell()
                    delta_t = (now - start_time).total_seconds()
                    bps = int(math.floor((cur_pos - prev_pos) / delta_t)) if delta_t > 0 else 0
                    prev_pos = cur_pos
                    start_time = now

                if bps > 0:
                    eta = int((file_size - f.tell()) / bps)

                if progress_cb is not None and file_size > 0:
                    percent = int(f.tell() * 100 / file_size)
                    progress_cb(percent, bps, eta)

                data_index += 1

                # commit batch
                if data_index % batch_size == 0:
                    conn.commit()
                    begin_tx()

            # commit finale
            conn.commit()
        except Exception:
            conn.rollback()
            logger.exception(
                "Errore durante la lettura del file di sessione %s", file_name
            )
            raise

    max_index = data_index

    # ------------------------------------------------------------------
    # Ricalcolo lista cadute (falls) da DB
    # ------------------------------------------------------------------
    try:
        for table in ("fastSensors", "slowSensors"):
            for row in conn.execute(
                f"SELECT dataIndex, activation FROM {table} "
                "WHERE activation != 0 ORDER BY dataIndex"
            ):
                falls.append(Fall(data_index=int(row[0]), activation=int(row[1])))
    except Exception as exc:
        logger.warning("Errore nel calcolo dei 'fall' dalla base dati: %s", exc)

    # ------------------------------------------------------------------
    # Timezone + min/max Timestamp da gpssensors
    # ------------------------------------------------------------------
    min_time: Optional[dt.datetime] = None
    max_time: Optional[dt.datetime] = None

    try:
        offset_hours = _get_timezone_offset_hours_from_db_bounds(conn, logger)
        if offset_hours is not None:
            tzoffset_expr = f"{offset_hours} hours"
            for table in ("gpssensors", "slowSensors", "fastSensors"):
                conn.execute(
                    f"""
                    UPDATE {table}
                    SET Timestamp = strftime('%Y-%m-%d %H:%M:%f', Timestamp, ?)
                    """,
                    (tzoffset_expr,),
                )
            conn.commit()

        row = conn.execute(
            "SELECT MIN(Timestamp), MAX(Timestamp) FROM gpssensors"
        ).fetchone()
        if row and row[0] is not None and row[1] is not None:
            # Assumo che Timestamp sia in formato ISO compatibile
            min_time = dt.datetime.fromisoformat(row[0])
            max_time = dt.datetime.fromisoformat(row[1])
    except Exception as exc:
        logger.warning(
            "Errore nel calcolo min/max Timestamp da gpssensors: %s", exc
        )

    return ReadSessionResult(
        max_index=max_index,
        falls=falls,
        gps_data=gps_data,
        min_time=min_time,
        max_time=max_time,
        session_header=session_header,
    )