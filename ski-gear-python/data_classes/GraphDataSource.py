from collections import defaultdict
from dataclasses import dataclass
from typing import Iterator, List, Optional


@dataclass
class DataPoint:
    x: float
    y: float

class GraphDataSource:
    def __init__(self):
        self.data: List[List[DataPoint]] = [[] for _ in range(24)]
        self.main: List[List[DataPoint]] = [[] for _ in range(6)]
        self.orientation: List[List[DataPoint]] = [[] for _ in range(3)]
        self.gravity: List[List[DataPoint]] = [[] for _ in range(3)]
        self.speedData: List[DataPoint] = []

    def get_sensor_data(self, sensor: int, axis: int) -> Iterator[DataPoint]:
        for dp in self.data[sensor * 3 + axis]:
            yield dp

    def get_main_data(self, sensor: int, axis: int) -> Iterator[DataPoint]:
        for dp in self.main[sensor * 3 + axis]:
            yield dp

    def get_speed_data(self) -> Iterator[DataPoint]:
        for dp in self.speedData:
            yield dp

    def get_gyro_data(self, axis: int) -> Iterator[DataPoint]:
        for dp in self.data[21 + axis]:
            yield dp

    def get_orientation_data(self, axis: int) -> Iterator[DataPoint]:
        for dp in self.orientation[axis]:
            yield dp

    def get_gravity_data(self, axis: int) -> Iterator[DataPoint]:
        for dp in self.gravity[axis]:
            yield dp

    def reset(self) -> None:
        self.data = [[] for _ in range(24)]
        self.main = [[] for _ in range(6)]
        self.gravity = [[] for _ in range(3)]
        self.orientation = [[] for _ in range(3)]
        self.speedData = []

    def get_data_count(self) -> int:
        return len(self.data[0]) if self.data else 0

    def add_sensor(self, sd) -> None:
        if sd.nofsensors == 2:
            for axis in range(3):
                self.main[axis].append(DataPoint(sd.index, sd.accelerometer[0][axis]))
            for i in range(3):      
                self.main[3 + i].append(DataPoint(sd.index, sd.gyro[i]))
            return

        for sensor in range(sd.nofsensors):
            for j in range(3):
                self.data[sensor * 3 + j].append(DataPoint(sd.index, sd.accelerometer[sensor][j]))

        if sd.nofsensors == 7:
            for k in range(3):
                self.data[sd.nofsensors * 3 + k].append(DataPoint(sd.index, sd.gyro[k]))

        if getattr(sd, "orientation", None) is not None:
            self.orientation[0].append(DataPoint(sd.index, sd.orientation[0]))
            self.orientation[1].append(DataPoint(sd.index, sd.orientation[1]))
            self.orientation[2].append(DataPoint(sd.index, sd.orientation[2]))

        if getattr(sd, "gravity", None) is not None:
            self.gravity[0].append(DataPoint(sd.index, sd.gravity[0]))
            self.gravity[1].append(DataPoint(sd.index, sd.gravity[1]))
            self.gravity[2].append(DataPoint(sd.index, sd.gravity[2]))

    def add_gps(self, sd) -> None:
        self.speedData.append(DataPoint(sd.index, sd.speed))

    def add(self, sd) -> None:
        if hasattr(sd, "speed"):
            self.add_gps(sd)
        else:
            self.add_sensor(sd)


