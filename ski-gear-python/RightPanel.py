from PySide6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QSlider, QLabel
from PySide6.QtCore import Qt, Signal
import Graph
import Utilities
from datetime import datetime, timedelta

class RightPanel(QWidget):
    timeline_changed = Signal(int)

    def __init__(self, map_widget):
        super().__init__()
        self.map = map_widget
        self.path_coords = []

        self.start_time: datetime | None = None
        self.min_index: int = 0
        self.end_time: datetime | None = None
        self.max_index: int = 0

        self._session_controller = None
        self._index_to_datetime = None  

        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        self.graph_frame = QWidget()
        layout.addWidget(self.graph_frame, 1)

        # Time labels row (start | current | end)
        time_container = QWidget()
        time_container.setStyleSheet("background-color: #222;")
        tc_layout = QHBoxLayout(time_container)
        tc_layout.setContentsMargins(6, 2, 6, 2)
        tc_layout.setSpacing(4)
        self.lbl_start = QLabel("--:--:--")
        self.lbl_current = QLabel("--:--:--")
        self.lbl_end = QLabel("--:--:--")
        self.lbl_start.setAlignment(Qt.AlignmentFlag.AlignLeft | Qt.AlignmentFlag.AlignVCenter)
        self.lbl_current.setAlignment(Qt.AlignmentFlag.AlignHCenter | Qt.AlignmentFlag.AlignVCenter)
        self.lbl_end.setAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
        style = "color: white; padding: 2px 6px;"
        self.lbl_start.setStyleSheet(style)
        self.lbl_current.setStyleSheet(style)
        self.lbl_end.setStyleSheet(style)
        tc_layout.addWidget(self.lbl_start, 1)
        tc_layout.addWidget(self.lbl_current, 1)
        tc_layout.addWidget(self.lbl_end, 1)
        layout.addWidget(time_container)

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
        # Ensure labels show something at startup
        self._refresh_time_labels()

    def on_timeline_change(self, slider_value: int):
        idx = int(slider_value)
        if 0 <= idx < len(self.path_coords):
            lat, lon = self.path_coords[idx]
            self.map.MoveMarker(lat, lon)
        # Update current time label
        self._update_current_time_label(idx)

    def add_path_coord(self, lat: float, lon: float):
        """Aggiunge una nuova coordinata (lat, lon) a path_coords."""
        self.path_coords.append((lat, lon))

    def update_timeline_range(self):
        self.timeline.setMaximum(len(self.path_coords) - 1)
        self.timeline.setTickInterval(max(1, len(self.path_coords)//10))
        self.timeline.setPageStep(max(1, len(self.path_coords)//20))
        self.timeline.setValue(0)
        # refresh labels when range changes
        self._refresh_time_labels()
        
    def resetPathCoords(self):
        self.path_coords = []
        self.update_timeline_range()

    def set_session_time_refs(self, start_time: datetime, min_index: int):
        self.start_time = start_time
        self.min_index = int(min_index)
        self._refresh_time_labels()
    
    def set_index_bounds(self, min_index: int, max_index: int):
        self.min_index = int(min_index)
        self.max_index = int(max_index)
        self._refresh_time_labels()

    def set_session_controller(self, session):
        self._session_controller = session

    # --- Time label helpers -------------------------------------------------
    def set_index_to_datetime(self, fn):
        """Optional: set a callable that maps an index to a datetime."""
        self._index_to_datetime = fn
        self._refresh_time_labels()

    def set_time_bounds(self, start_time: datetime | None, end_time: datetime | None):
        """Explicitly set start and end time labels."""
        self.start_time = start_time
        self.end_time = end_time
        self._refresh_time_labels()

    def _format_dt(self, dt: datetime | None) -> str:
        if dt is None:
            return "--:--:--"
        try:
            return dt.strftime("%H:%M:%S")
        except Exception:
            return str(dt)

    def _idx_to_dt(self, idx: int) -> datetime | None:
        if callable(self._index_to_datetime):
            try:
                return self._index_to_datetime(idx)
            except Exception:
                pass
        if self.start_time is None:
            return None
        if self.end_time is None or self.max_index <= self.min_index:
            return self.start_time
        tmax = max(1, self.timeline.maximum())
        frac = float(idx) / float(tmax)
        data_idx = int(self.min_index + frac * (self.max_index - self.min_index))
        rel = (data_idx - self.min_index) / float(self.max_index - self.min_index)
        rel = max(0.0, min(1.0, rel))
        span = self.end_time - self.start_time
        return self.start_time + timedelta(seconds=span.total_seconds() * rel)

    def _refresh_time_labels(self):
        # Start
        self.lbl_start.setText(self._format_dt(self.start_time))
        # End: try last index
        end_dt = self.end_time
        if end_dt is None:
            last_idx = len(self.path_coords) - 1
            if last_idx >= 0:
                end_dt = self._idx_to_dt(last_idx)
        self.lbl_end.setText(self._format_dt(end_dt))
        # Current
        self._update_current_time_label(self.timeline.value())

    def _update_current_time_label(self, idx: int):
        cur_dt = self._idx_to_dt(idx)
        self.lbl_current.setText(self._format_dt(cur_dt))