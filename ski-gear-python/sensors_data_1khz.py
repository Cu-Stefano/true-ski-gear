from dataclasses import dataclass
import struct
from typing import BinaryIO
try:
    from .data_header import DataHeader
    from .timestamp import TimeStamp
    from .binary_utils import read_exact
except ImportError:
    from data_header import DataHeader
    from timestamp import TimeStamp
    from binary_utils import read_exact

# Complete layout (Pack=1):
# DataHeader (2) + TimeStamp (4) + gyro(3*short=6) + acc(3*short=6) + activation(uint=4) = 22
_STRUCT_FULL = struct.Struct("<BBI3h3hI")  # We will parse manually for clarity

@dataclass
class SensorsData1KHZ:
    gyro: tuple  # (gx, gy, gz)
    acc: tuple   # (ax, ay, az)
    activation: int

@dataclass
class SensorsData1KHZStruct:
    header: DataHeader
    t: TimeStamp
    data: SensorsData1KHZ

    @classmethod
    def from_file(cls, f: BinaryIO) -> "SensorsData1KHZStruct":
        # Read entire struct at once to minimize file pointer ops
        raw = read_exact(f, _STRUCT_FULL.size)
        # Unpack "BBI3h3hI"
        (type_, size_,
         msec,
         g0, g1, g2,
         a0, a1, a2,
         activation) = _STRUCT_FULL.unpack(raw)
        header = DataHeader(type=type_, size=size_)
        if header.type != 13:
            raise ValueError(f"Unexpected SensorsData1KHZStruct type {header.type} != 13")
        # size should equal remainder after DataHeader => timestamp(4)+payload(6+6+4)=20
        expected_size = 20
        if header.size != expected_size:
            raise ValueError(f"Unexpected size field {header.size} (expected {expected_size})")
        t = TimeStamp(msec=msec)
        data = SensorsData1KHZ(gyro=(g0, g1, g2), acc=(a0, a1, a2), activation=activation)
        return cls(header=header, t=t, data=data)