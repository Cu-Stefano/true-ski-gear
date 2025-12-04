import os
import sqlite3
from abc import ABC, abstractmethod
from datetime import datetime, timedelta
from typing import List, Iterable
from data_classes.GPSData import GPSData
from data_classes.GraphDataSource import DataPoint
from dataclasses import dataclass
import data_classes.GraphDataSource as GraphDataSource

@dataclass
class Tag:
    id: int
    type: str
    description: str
    timestamp: datetime

    # Optional: convenience constructor if you want current timestamp by default
    @classmethod
    def create(cls, id: int, type: str, description: str) -> "Tag":
        return cls(id=id, type=type, description=description, timestamp=datetime.utcnow())

class BaseSession(ABC):
    SESSION_V1 = 1
    SESSION_V2 = 2

    MAX_GRAPHS = 9
    MAX_SENSORS = 7

    gyro_index = 5
    speed_index = 6

    def __init__(self, nofgraphs: int, nofsensors: int):
        self.session_version = 1
        self.nofgraphs = min(self.MAX_GRAPHS, nofgraphs)
        self.nofsensors = min(self.MAX_SENSORS, nofsensors)

        self.falls: List = []
        self.gps_data: List[GPSData] = []

        self.min_time: datetime | None = None
        self.max_time: datetime | None = None

        self.min_index: int = 0
        self.max_index: int = 0

        self.conn: sqlite3.Connection | None = None
        self.graph_data = GraphDataSource.GraphDataSource()

    # -------------------------------
    # Range helpers
    # -------------------------------
    def get_session_range(self) -> tuple[int, int]:
        return (self.min_index, self.max_index + 50)

    # -------------------------------
    # GPS helpers
    # -------------------------------
    @property
    def gps_count(self) -> int:
        return len(self.gps_data)

    def get_time_for_index(self, idx: int) -> datetime:
        if self.min_time is None:
            raise ValueError("min_time not initialized")
        return self.min_time + timedelta(milliseconds=(idx - self.min_index))

    def get_gps_index(self, idx: int) -> int:
        for i in range(len(self.gps_data) - 1):
            if self.gps_data[i+1].index >= idx:
                return i
        return max(0, len(self.gps_data) - 1)

    def get_last_gps(self) -> GPSData | None:
        return self.gps_data[-1] if self.gps_data else None

    def get_db_filename(self) -> str:
        if self.conn is None:
            raise ValueError("Database connection is not initialized")
        return self.conn.execute("PRAGMA database_list").fetchone()[2]

    # -------------------------------
    # Derived classes must implement:
    # -------------------------------
    @abstractmethod
    def load_data(self, min_time: int, max_time: int):
        pass

    @abstractmethod
    def close_db(self):
        pass

    @abstractmethod
    def commit(self):
        pass

    @abstractmethod
    def export_from_to(self, filename: str, min_idx: int, max_idx: int):
        pass

    # -------------------------------
    # Sensor wrappers (GraphDataSource)
    # -------------------------------
    def get_sensor_data(self, sensor: int, axis: int) -> Iterable[DataPoint]:
        return self.graph_data.get_sensor_data(sensor, axis)

    def get_main_data(self, sensor: int, axis: int) -> Iterable[DataPoint]:
        # axis retained for compatibility; underlying method expects only one positional argument
        return self.graph_data.get_main_data(sensor, axis)

    def get_speed_data(self) -> Iterable[DataPoint]:
        return self.graph_data.get_speed_data()

    def get_gyro_data(self, axis: int) -> Iterable[DataPoint]:
        return self.graph_data.get_gyro_data(axis)

    # -------------------------------
    # Tags fetching
    # -------------------------------
    def get_tags(self):
        if self.conn is None:
            raise ValueError("Database connection is not initialized")
        result = []
        cur = self.conn.cursor()
        cur.execute("SELECT id, type, description, Timestamp FROM tags")

        for row in cur.fetchall():
            tag_id, ttype, descr, timestamp = row
            result.append(Tag(tag_id, ttype, descr, datetime.fromisoformat(timestamp)))

        return result

    # -------------------------------
    # Latitude/longitude min/max
    # -------------------------------
    def get_max_lat(self):
        return max((g.coords.Lat for g in self.gps_data), default=0.0)

    def get_min_lat(self):
        return min((g.coords.Lat for g in self.gps_data), default=0.0)

    def get_max_lng(self):
        return max((g.coords.Lng for g in self.gps_data), default=0.0)

    def get_min_lng(self):
        return min((g.coords.Lng for g in self.gps_data), default=0.0)

    @staticmethod
    def get_db_folder() -> str:
        appdata = os.getenv("APPDATA")
        if appdata is None:
            raise EnvironmentError("APPDATA environment variable not set")
        return os.path.join(appdata, "BetterFish", "db")
