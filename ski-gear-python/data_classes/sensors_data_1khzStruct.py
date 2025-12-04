import struct
from dataclasses import dataclass
from .data_header import DataHeader
from .timestamp import TimeStamp
from .sensors_data_1khz import SensorsData1KHZ

@dataclass
class SensorsData1KHZStruct:
    header: DataHeader
    t: TimeStamp
    data: SensorsData1KHZ

    @classmethod
    def parse(cls, reader):
        header = DataHeader.parse(reader)
        timestamp = TimeStamp.parse(reader)
        data = SensorsData1KHZ.parse(reader)
        return cls(header, timestamp, data)
