from dataclasses import dataclass
import struct

# TimeStamp: uint msec
_TIMESTAMP_STRUCT = struct.Struct("<I")

@dataclass
class TimeStamp:
    msec: int

    @classmethod
    def parse(cls, b: bytes, offset=0):
        if len(b) < offset + _TIMESTAMP_STRUCT.size:
            raise ValueError("Not enough bytes for TimeStamp")
        (msec,) = _TIMESTAMP_STRUCT.unpack_from(b, offset)
        return cls(msec)