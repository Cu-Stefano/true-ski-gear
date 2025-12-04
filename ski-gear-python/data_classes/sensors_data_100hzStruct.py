from dataclasses import dataclass
from .data_header import DataHeader
from .timestamp import TimeStamp
from .sensors_data_100hz import SensorsData100HZ

@dataclass
class SensorsData100HZStruct:
    header: DataHeader
    t: TimeStamp
    data: SensorsData100HZ

    @classmethod
    def parse(cls, reader):

        header = DataHeader.parse(reader)
        timestamp = TimeStamp.parse(reader)
        data = SensorsData100HZ.parse(reader)

        return cls(header, timestamp, data)
