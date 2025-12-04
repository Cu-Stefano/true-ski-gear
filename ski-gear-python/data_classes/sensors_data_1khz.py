from dataclasses import dataclass
import struct

@dataclass
class SensorsData1KHZ:
    gyro: list
    acc: list
    activation: int

    FORMAT = "<3h3hI"   # 3 short + 3 short + uint
    SIZE = struct.calcsize(FORMAT)

    @classmethod
    def parse(cls, reader):
        data = reader.read(cls.SIZE)
        values = struct.unpack(cls.FORMAT, data)
        gyro = list(values[0:3])
        acc = list(values[3:6])
        activation = values[6]
        return cls(gyro, acc, activation)
