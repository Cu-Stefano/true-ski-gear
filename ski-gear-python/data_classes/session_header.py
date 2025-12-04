import struct
from .data_header import DataHeader
from dataclasses import dataclass

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

    FORMAT = (
        "<"     # little endian
        "BB"    # DataHeader
        "III"   # id, board_id, fw_version
        "BBB"   # day, month, year
        "HHHH"  # acc*, ext_acc*
        "B"     # ext_status
        "HHHH"  # gyro*, mag*
        "H"     # gps_rate
        "I"     # activations
    )

    SIZE = struct.calcsize(FORMAT)

    @classmethod
    def parse(cls, reader):
        data = reader.read(cls.SIZE)
        values = struct.unpack(cls.FORMAT, data)
        header = DataHeader(values[0], values[1])
        return cls(header, *values[2:])
