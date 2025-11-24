from dataclasses import dataclass
import struct
try:
    from .data_header import DataHeader
    from .timestamp import TimeStamp
except ImportError:
    from data_header import DataHeader
    from timestamp import TimeStamp

# Full (current) struct size expected: 76 bytes
# DataHeader(2) + TimeStamp(4)
# acc(12h=24) + mag(3h=6) + longitude(float=4) + latitude(float=4) + speed(float=4)
# rotation(3f=12) + gravity(3f=12) + activation(uint=4)
# Payload after timestamp starts at offset 6
# Format full: <12h3hfff3f3fI
_FULL_100HZ_STRUCT = struct.Struct("<12h3hfff3f3fI")

# Legacy (short) variant without rotation/gravity?
# If header.size < 74 we fallback to: acc + mag + lon + lat + speed + activation
# acc(24)+mag(6)+lon(4)+lat(4)+speed(4)+activation(4)=46 bytes payload + 6 header = 52 total
_LEGACY_100HZ_STRUCT = struct.Struct("<12h3hfffI")

@dataclass
class SensorsData100HZ:
    acc: tuple          # 12 raw shorts
    mag: tuple          # 3 raw shorts
    longitude: float
    latitude: float
    speed: float
    rotation: tuple     # 3 floats (may be empty for legacy)
    gravity: tuple      # 3 floats (may be empty for legacy)
    activation: int

@dataclass
class SensorsData100HZStruct:
    header: DataHeader
    t: TimeStamp
    data: SensorsData100HZ

    @classmethod
    def parse(cls, b: bytes):
        if len(b) < 52:
            raise ValueError("Not enough bytes for 100Hz struct (minimum legacy size)")
        header = DataHeader.parse(b[0:2])
        ts = TimeStamp.parse(b, 2)
        if header.size >= 74 and len(b) >= 76:
            unpacked = _FULL_100HZ_STRUCT.unpack_from(b, 6)
            acc = unpacked[0:12]
            mag = unpacked[12:15]
            longitude, latitude, speed = unpacked[15:18]
            rotation = unpacked[18:21]
            gravity = unpacked[21:24]
            activation = unpacked[24]
            data = SensorsData100HZ(acc=acc, mag=mag, longitude=longitude, latitude=latitude,
                                    speed=speed, rotation=rotation, gravity=gravity,
                                    activation=activation)
        else:
            unpacked = _LEGACY_100HZ_STRUCT.unpack_from(b, 6)
            acc = unpacked[0:12]
            mag = unpacked[12:15]
            longitude, latitude, speed = unpacked[15:18]
            activation = unpacked[18]
            data = SensorsData100HZ(acc=acc, mag=mag, longitude=longitude, latitude=latitude,
                                    speed=speed, rotation=(), gravity=(), activation=activation)
        return cls(header=header, t=ts, data=data)