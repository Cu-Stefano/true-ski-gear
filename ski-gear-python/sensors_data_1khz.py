from dataclasses import dataclass
import struct
try:
    from .data_header import DataHeader
    from .timestamp import TimeStamp
except ImportError:
    from data_header import DataHeader
    from timestamp import TimeStamp

# SensorsData1KHZStruct total:
# DataHeader(2) + TimeStamp(4) + gyro(3*short=6) + acc(3*short=6) + activation(uint=4) = 22 bytes
# Data portion format: <3h3hI
_DATA_1KHZ_PAYLOAD = struct.Struct("<3h3hI")

@dataclass
class SensorsData1KHZ:
    gyro: tuple   # (gx, gy, gz) raw short
    acc: tuple    # (ax, ay, az) raw short
    activation: int

@dataclass
class SensorsData1KHZStruct:
    header: DataHeader
    t: TimeStamp
    data: SensorsData1KHZ

    @classmethod
    def parse(cls, b: bytes):
        if len(b) < 22:
            raise ValueError("Not enough bytes for 1kHz struct")
        header = DataHeader.parse(b[0:2])
        ts = TimeStamp.parse(b, 2)
        gx, gy, gz, ax, ay, az, activation = _DATA_1KHZ_PAYLOAD.unpack_from(b, 6)
        data = SensorsData1KHZ(gyro=(gx, gy, gz), acc=(ax, ay, az), activation=activation)
        return cls(header=header, t=ts, data=data)