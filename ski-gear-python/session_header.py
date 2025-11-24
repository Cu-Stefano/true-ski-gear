from dataclasses import dataclass
import struct
try:
    from .data_header import DataHeader
except ImportError:
    from data_header import DataHeader

# After DataHeader (2 bytes), 38 bytes:
# <III3B4HB5HI
_SESSION_HEADER_STRUCT = struct.Struct("<III3B4HB5HI")

@dataclass
class SessionHeader:
    header: DataHeader
    id: int
    board_id: int
    fw_version: int
    day: int
    month: int
    year: int
    acc_full_scale: int
    acc_rate: int
    ext_acc_full_scale: int
    ext_acc_rate: int
    ext_status: int
    gyro_full_scale: int
    gyro_rate: int
    mag_full_scale: int
    mag_rate: int
    gps_rate: int
    activations: int

    @classmethod
    def parse(cls, b: bytes):
        if len(b) < 40:
            raise ValueError("Not enough bytes for SessionHeader")
        header = DataHeader.parse(b[0:2])
        (id_, board_id, fw_version,
         day, month, year,
         acc_full_scale, acc_rate, ext_acc_full_scale, ext_acc_rate,
         ext_status,
         gyro_full_scale, gyro_rate, mag_full_scale, mag_rate, gps_rate,
         activations) = _SESSION_HEADER_STRUCT.unpack_from(b, 2)
        return cls(header=header, id=id_, board_id=board_id, fw_version=fw_version,
                   day=day, month=month, year=year,
                   acc_full_scale=acc_full_scale, acc_rate=acc_rate,
                   ext_acc_full_scale=ext_acc_full_scale, ext_acc_rate=ext_acc_rate,
                   ext_status=ext_status,
                   gyro_full_scale=gyro_full_scale, gyro_rate=gyro_rate,
                   mag_full_scale=mag_full_scale, mag_rate=mag_rate,
                   gps_rate=gps_rate, activations=activations)