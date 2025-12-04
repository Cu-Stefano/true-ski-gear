from dataclasses import dataclass
import struct

@dataclass
class TimeStamp:
    msec: int

    FORMAT = "<I"     # uint32
    SIZE = struct.calcsize(FORMAT)

    @classmethod
    def parse(cls, reader):
        data = reader.read(cls.SIZE)
        (msec,) = struct.unpack(cls.FORMAT, data)
        return cls(msec)
