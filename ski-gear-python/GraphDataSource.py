from collections import defaultdict
from dataclasses import dataclass

@dataclass
class DataPoint:
    x: float
    y: float
    
class GraphDataSource:
    def __init__(self):
        # sensor_data[sensor][axis] -> list[DataPoint]
        self.sensor_data = defaultdict(lambda: defaultdict(list))

        # main_data[axis] -> list[DataPoint]
        self.main_data = defaultdict(list)

        # speed_data -> list[DataPoint]
        self.speed_data = []

        # gyro_data[axis] -> list[DataPoint]
        self.gyro_data = defaultdict(list)

    # ---------------------------------------------------
    # Adders
    # ---------------------------------------------------
    def add_sensor_data(self, sensor: int, axis: int, point: DataPoint):
        self.sensor_data[sensor][axis].append(point)

    def add_main_data(self, axis: int, point: DataPoint):
        self.main_data[axis].append(point)

    def add_speed_data(self, point: DataPoint):
        self.speed_data.append(point)

    def add_gyro_data(self, axis: int, point: DataPoint):
        self.gyro_data[axis].append(point)

    # ---------------------------------------------------
    # Getters
    # ---------------------------------------------------
    def get_sensor_data(self, sensor: int, axis: int):
        return self.sensor_data[sensor][axis]

    def get_main_data(self, axis: int):
        return self.main_data[axis]

    def get_speed_data(self):
        return self.speed_data

    def get_gyro_data(self, axis: int):
        return self.gyro_data[axis]
