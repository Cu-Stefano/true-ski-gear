from dataclasses import dataclass
import struct

@dataclass
class SensorsData100HZ:
    acc: list
    mag: list
    longitude: float
    latitude: float
    speed: float
    rotation: list
    gravity: list
    activation: int

    FORMAT = (
        "<"
        "12h"   # acc[]
        "3h"    # mag[]
        "fff"   # longitude, latitude, speed
        "3f"    # rotation[]
        "3f"    # gravity[]
        "I"     # activation
    )

    SIZE = struct.calcsize(FORMAT)

    @classmethod
    def parse(cls, reader):
        data = reader.read(cls.SIZE)
        values = list(struct.unpack(cls.FORMAT, data))

        acc = values[0:12]
        mag = values[12:15]
        longitude, latitude, speed = values[15:18]
        rotation = values[18:21]
        gravity = values[21:24]
        activation = values[24]

        return cls(acc, mag, longitude, latitude, speed, rotation, gravity, activation)
