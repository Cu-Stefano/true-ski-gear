from __future__ import annotations
from dataclasses import dataclass, field
from datetime import datetime

@dataclass
class GPSCoords:
    Lat: float = 0.0
    Lng: float = 0.0

@dataclass
class GPSData:
    index: int
    time: datetime
    speed: float
    angle: float = 0.0
    coords: GPSCoords = field(default_factory=GPSCoords)

    def __init__(self, index: int, time: datetime, speed: float):
        self.index = index
        self.time = time
        self.speed = speed
        self.angle = 0.0
        self.coords = GPSCoords()

    def to_dict(self) -> dict:
        return {
            "index": self.index,
            "time": self.time.isoformat(),
            "speed": self.speed,
            "angle": self.angle,
            "lat": self.coords.Lat,
            "lng": self.coords.Lng
        }