from typing import BinaryIO

class EOFErrorWithContext(EOFError):
    pass

def read_exact(f: BinaryIO, size: int) -> bytes:
    data = f.read(size)
    if len(data) != size:
        raise EOFErrorWithContext(f"Expected {size} bytes, got {len(data)} at pos {f.tell() - len(data)}")
    return data