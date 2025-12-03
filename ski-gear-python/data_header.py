import struct
from dataclasses import dataclass

@dataclass
class DataHeader:
    type: int
    size: int

    FORMAT = "<BB"      # 1 byte + 1 byte = 2 bytes
    SIZE = struct.calcsize(FORMAT)
    
    @classmethod
    def parse(cls, reader):
        data = reader.read(cls.SIZE)
        type_, size = struct.unpack(cls.FORMAT, data)
        return cls(type_, size)