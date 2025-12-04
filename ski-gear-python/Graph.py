from typing import Iterable, Sequence
from PySide6.QtWidgets import QWidget, QVBoxLayout, QLabel
from PySide6.QtCore import Qt, Signal
import pyqtgraph as pg
import numpy as np
from datetime import datetime, timedelta

class NoRightZoomViewBox(pg.ViewBox):
    clicked = Signal(float)

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._x_padding = 0.0
        self._y_padding = 0.25

    def suggestPadding(self, axis):
        try:
            return self._x_padding if axis == 0 else self._y_padding
        except Exception:
            return 0.0

    def setYPadding(self, frac: float):
        try:
            self._y_padding = max(0.0, float(frac))
        except Exception:
            pass


class Graph(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        pg.setConfigOptions(antialias=True)
        self.plotw = pg.PlotWidget(viewBox=NoRightZoomViewBox(), background='k')
        self.plotw.showGrid(x=True, y=True, alpha=0.3)

        self.plotw.getViewBox().setMouseEnabled(x=True, y=False)

        self.curve = self.plotw.plot([], [], pen=pg.mkPen('y', width=0.8), name='signal')

        layout.addWidget(self.plotw)

        # Pose bottom time label
        self.pose_time_label = QLabel("--:--:--", self)
        self.pose_time_label.setAlignment(Qt.AlignRight)
        self.pose_time_label.setStyleSheet("color: white; padding: 2px 4px;")
        layout.addWidget(self.pose_time_label)

        self._start_time: datetime | None = None
        self._min_index: int = 0

    def plot_example(self):
        self.curve.setData([], [])
        self.plotw.setTitle("", color='w')
        self.plotw.enableAutoRange(axis=pg.ViewBox.XYAxes, enable=True)
        self.pose_time_label.setText("--:--:--")

    def _format_time(self, index: int) -> str:
        try:
            if self._start_time is None:
                # Fallback: show index as seconds
                seconds = max(0, int(index) - int(self._min_index))
                h = seconds // 3600
                m = (seconds % 3600) // 60
                s = seconds % 60
                return f"{h:02d}:{m:02d}:{s:02d}"
            delta_ms = max(0, int(index) - int(self._min_index))
            # Treat index as milliseconds offset from min_index
            dt = self._start_time + timedelta(milliseconds=delta_ms)
            return dt.strftime("%H:%M:%S")
        except Exception:
            return "--:--:--"