from typing import Iterable, Sequence
from PySide6.QtWidgets import QWidget, QVBoxLayout
from PySide6.QtCore import Qt, Signal
import pyqtgraph as pg
import numpy as np

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

        self.cursor = pg.InfiniteLine(angle=90, movable=True, pen=pg.mkPen('w', width=0.8))
        self.plotw.addItem(self.cursor)

        self.plotw.getViewBox().clicked.connect(self.update_cursor_pose)

        layout.addWidget(self.plotw)

    def update_cursor_pose(self, x):
        self.cursor.setValue(x)

    def plot_example(self):
        self.curve.setData([], [])
        self.plotw.setTitle("", color='w')
        self.cursor.setValue(0)
        self.plotw.enableAutoRange(axis=pg.ViewBox.XYAxes, enable=True)
