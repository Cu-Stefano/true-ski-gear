from dataclasses import dataclass
import struct
from typing import BinaryIO
try:
    from .data_header import DataHeader
    from .binary_utils import read_exact
except ImportError:
    from data_header import DataHeader
    from binary_utils import read_exact

_AFTER_HEADER_STRUCT = struct.Struct("<III3B4HB5HI")
TOTAL_SIZE = DataHeader.__annotations__ and 2 + _AFTER_HEADER_STRUCT.size  # 2 + 38 = 40

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
    def from_file(cls, f: BinaryIO) -> "SessionHeader":
        # Read DataHeader
        dh_raw = read_exact(f, 2)
        header = DataHeader.parse(dh_raw)
        if header.type != 4:
            raise ValueError(f"Unexpected SessionHeader type {header.type} != 4")
        # header.size should be size of (TimeStamp + payload) for other structs, but
        # for SessionHeader we expect fixed total size (40). Accept either 40 or 38 (without DataHeader).
        if header.size not in (38, 40):
            # Accept legacy writer mistakes only if you decide so; here we enforce.
            raise ValueError(f"Unexpected SessionHeader size field {header.size}")
        rest = read_exact(f, _AFTER_HEADER_STRUCT.size)
        (id_,
         board_id,
         fw_version,
         day,
         month,
         year,
         acc_full_scale,
         acc_rate,
         ext_acc_full_scale,
         ext_acc_rate,
         ext_status,
         gyro_full_scale,
         gyro_rate,
         mag_full_scale,
         mag_rate,
         gps_rate,
         activations) = _AFTER_HEADER_STRUCT.unpack(rest)
        return cls(header, id_, board_id, fw_version,
                   day, month, year,
                   acc_full_scale, acc_rate,
                   ext_acc_full_scale, ext_acc_rate,
                   ext_status,
                   gyro_full_scale, gyro_rate,
                   mag_full_scale, mag_rate,
                   gps_rate, activations)