from typing import Iterable, Sequence
from PySide6.QtWidgets import QWidget, QVBoxLayout
from PySide6.QtCore import Qt, Signal
import pyqtgraph as pg
import numpy as np

class NoRightZoomViewBox(pg.ViewBox):
    clicked = Signal(float)  # Segnale emesso con la posizione x del click

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

    def mouseDragEvent(self, ev, axis=None):
        if ev.button() == Qt.RightButton:
            # Pan solo sull'asse X
            diff = ev.pos() - ev.lastPos()
            dx = diff.x() * 0.005  # riduci la sensibilità 
            self.translateBy(x=-dx, y=0)
            ev.accept()
        else:
            pos = self.mapToView(ev.pos())
            self.clicked.emit(pos.x())  # Emetti segnale con posizione x
            ev.accept()
            
    def mouseClickEvent(self, ev):
        if ev.button() == Qt.LeftButton:
            pos = self.mapToView(ev.pos())
            self.clicked.emit(pos.x())  # Emetti segnale con posizione x
            ev.accept()


class Graph(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        # PlotWidget con ViewBox personalizzata
        pg.setConfigOptions(antialias=True)
        self.plotw = pg.PlotWidget(viewBox=NoRightZoomViewBox(), background='k')
        self.plotw.showGrid(x=True, y=True, alpha=0.3)

        # rotella: zoom, mouse destro: pan, disabilita pan su Y
        self.plotw.getViewBox().setMouseEnabled(x=True, y=False)

        self.curve = self.plotw.plot([], [], pen=pg.mkPen('y', width=1), name='signal')

        self.cursor = pg.InfiniteLine(angle=90, movable=True, pen=pg.mkPen('w', width=1))
        self.plotw.addItem(self.cursor)

        self.plotw.getViewBox().clicked.connect(self.update_cursor)

        layout.addWidget(self.plotw)

    def update_cursor(self, x):
        self.cursor.setValue(x)

    def plot_example(self):
        t = np.linspace(0, 10, 500)
        y = np.sin(t) * 50 + 100
        self.curve.setData(t, y)
        self.plotw.setTitle("Grafico (unico, giallo)", color='w')
        self.cursor.setValue(t[len(t)//2])
        self.plotw.enableAutoRange(axis=pg.ViewBox.XYAxes, enable=True)
