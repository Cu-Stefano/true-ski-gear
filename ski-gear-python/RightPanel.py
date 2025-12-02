from PySide6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QSlider
from PySide6.QtCore import Qt
import Graph
import Utilities

class RightPanel(QWidget):
    def __init__(self, parent, map_widget):
        super().__init__(parent)
        self.map = map_widget

        base_lat = 46.4983
        base_lon = 11.3548
        step_lat = 0.00005
        step_lon = 0.00008
        self.path_coords = [(base_lat + i*step_lat, base_lon + i*step_lon) for i in range(100)]

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
        self.timeline.valueChanged.connect(self.on_timeline_next)
        bottom_layout.addWidget(self.timeline)

        self.timeline_next_btn = Utilities.createButton(">>", lambda: self.on_timeline_next(self.timeline.value()))
        bottom_layout.addWidget(self.timeline_next_btn)

        layout.addLayout(bottom_layout)
        self.setLayout(layout)

    def on_timeline_next(self, slider_value: int):
        idx = int(slider_value)
        if 0 <= idx < len(self.path_coords):
            lat, lon = self.path_coords[idx]
            self.map.MoveMarker(lat, lon)
