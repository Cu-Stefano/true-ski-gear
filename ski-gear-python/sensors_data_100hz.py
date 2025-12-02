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

# Full variant size after DataHeader: 74 (timestamp(4)+payload(70))
# Legacy variant size after DataHeader: 50 (timestamp(4)+payload(46))

# Payload (full) after timestamp:
# acc(12h) mag(3h) longitude(float) latitude(float) speed(float)
# rotation(3f) gravity(3f) activation(uint)
_FULL_PAYLOAD_STRUCT = struct.Struct("<12h3hfff3f3fI")
# Legacy payload (no rotation/gravity):
_LEGACY_PAYLOAD_STRUCT = struct.Struct("<12h3hfffI")

@dataclass
class SensorsData100HZ:
    acc: tuple           # 12 shorts
    mag: tuple           # 3 shorts
    longitude: float
    latitude: float
    speed: float
    rotation: tuple      # 3 floats (empty for legacy)
    gravity: tuple       # 3 floats (empty for legacy)
    activation: int

@dataclass
class SensorsData100HZStruct:
    header: DataHeader
    t: TimeStamp
    data: SensorsData100HZ

    @classmethod
    def from_file(cls, f: BinaryIO) -> "SensorsData100HZStruct":
        # Read DataHeader first
        dh_raw = read_exact(f, 2)
        header = DataHeader.parse(dh_raw)
        if header.type != 14:
            raise ValueError(f"Unexpected SensorsData100HZStruct type {header.type} != 14")

        # Determine which variant to parse based on header.size
        if header.size >= 74:
            # Full variant
            rest = read_exact(f, header.size)  # size includes timestamp + payload
            if len(rest) != header.size:
                raise EOFError("Incomplete 100Hz full record")
            # First 4 bytes = timestamp
            (msec,) = struct.unpack_from("<I", rest, 0)
            payload = rest[4:]
            if len(payload) != _FULL_PAYLOAD_STRUCT.size:
                raise ValueError(f"Payload size mismatch (got {len(payload)}, expected {_FULL_PAYLOAD_STRUCT.size})")
            unpacked = _FULL_PAYLOAD_STRUCT.unpack(payload)
            acc = unpacked[0:12]
            mag = unpacked[12:15]
            longitude, latitude, speed = unpacked[15:18]
            rotation = unpacked[18:21]
            gravity = unpacked[21:24]
            activation = unpacked[24]
            t = TimeStamp(msec=msec)
            data = SensorsData100HZ(
                acc=acc,
                mag=mag,
                longitude=longitude,
                latitude=latitude,
                speed=speed,
                rotation=rotation,
                gravity=gravity,
                activation=activation
            )
            return cls(header=header, t=t, data=data)
        else:
            # Legacy variant (header.size expected == 50)
            if header.size != 50:
                raise ValueError(f"Unexpected legacy size {header.size} (expected 50)")
            rest = read_exact(f, header.size)
            (msec,) = struct.unpack_from("<I", rest, 0)
            payload = rest[4:]
            if len(payload) != _LEGACY_PAYLOAD_STRUCT.size:
                raise ValueError(f"Legacy payload size mismatch (got {len(payload)}, expected {_LEGACY_PAYLOAD_STRUCT.size})")
            unpacked = _LEGACY_PAYLOAD_STRUCT.unpack(payload)
            acc = unpacked[0:12]
            mag = unpacked[12:15]
            longitude, latitude, speed = unpacked[15:18]
            activation = unpacked[18]
            t = TimeStamp(msec=msec)
            data = SensorsData100HZ(
                acc=acc,
                mag=mag,
                longitude=longitude,
                latitude=latitude,
                speed=speed,
                rotation=(),   # not present
                gravity=(),    # not present
                activation=activation
            )
            return cls(header=header, t=t, data=data)