from PySide6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QSlider
from PySide6.QtCore import Qt, Signal
import Graph
import Utilities
from datetime import datetime

class RightPanel(QWidget):
    timeline_changed = Signal(int)

    def __init__(self, parent, map_widget):
        super().__init__(parent)
        self.map = map_widget
        self.path_coords = []

        self.start_time: datetime | None = None
        self.min_index: int = 0

        self._session_controller = None

        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        self.graph_frame = Graph.Graph(self)
        layout.addWidget(self.graph_frame, 1)
        self.graph_frame.plot_example()

        bottom_layout = QHBoxLayout()
        
        self.timeline = QSlider(Qt.Orientation.Horizontal)
        self.timeline.setMinimum(0)
        self.timeline.setMaximum(len(self.path_coords) - 1)
        self.timeline.setTickPosition(QSlider.TickPosition.TicksBelow)
        self.timeline.setTickInterval(max(1, len(self.path_coords)//10))
        self.timeline.setPageStep(max(1, len(self.path_coords)//20))
        self.timeline.valueChanged.connect(self.timeline_changed.emit)
        self.timeline_changed.connect(self.on_timeline_change)
        bottom_layout.addWidget(self.timeline)

        self.timeline_next_btn = Utilities.createButton(
            ">>", lambda: self.timeline_changed.emit(self.timeline.value())
        )
        bottom_layout.addWidget(self.timeline_next_btn)

        layout.addLayout(bottom_layout)
        self.setLayout(layout)

    def on_timeline_change(self, slider_value: int):
        idx = int(slider_value)
        if 0 <= idx < len(self.path_coords):
            lat, lon = self.path_coords[idx]
            self.map.MoveMarker(lat, lon)

    def add_path_coord(self, lat: float, lon: float):
        """Aggiunge una nuova coordinata (lat, lon) a path_coords."""
        self.path_coords.append((lat, lon))

    def update_timeline_range(self):
        self.timeline.setMaximum(len(self.path_coords) - 1)
        self.timeline.setTickInterval(max(1, len(self.path_coords)//10))
        self.timeline.setPageStep(max(1, len(self.path_coords)//20))
        self.timeline.setValue(0)
        
    def resetPathCoords(self):
        self.path_coords = []
        self.update_timeline_range()

    def set_session_time_refs(self, start_time: datetime, min_index: int):
        self.start_time = start_time
        self.min_index = int(min_index)

    def set_session_controller(self, session):
        self._session_controller = session