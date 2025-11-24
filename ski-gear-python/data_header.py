from dataclasses import dataclass
import struct

# C# DataHeader: byte type; byte size
# LayoutKind.Sequential, Pack=1 => no padding
# Format: <BB (little-endian)
_DATA_HEADER_STRUCT = struct.Struct("<BB")

@dataclass
class DataHeader:
    type: int
    size: int

    @classmethod
    def parse(cls, b: bytes):
        if len(b) < _DATA_HEADER_STRUCT.size:
            raise ValueError("Not enough bytes for DataHeader")
        t, s = _DATA_HEADER_STRUCT.unpack_from(b, 0)
        return cls(t, s)