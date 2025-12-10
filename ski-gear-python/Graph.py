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

        # Evita zoom verticale e l'auto-range finché non impostiamo noi i limiti
        self.plotw.getViewBox().setMouseEnabled(x=True, y=False)
        self.plotw.enableAutoRange(axis=pg.ViewBox.XYAxes, enable=False)

        self.curve = self.plotw.plot([], [], pen=pg.mkPen('y', width=0.8), name='signal')
        # Performance: render only what is visible and let pyqtgraph downsample
        try:
            self.curve.setClipToView(True)
            self.curve.setAutoDownsample(True)
            # Keep mode conservative to avoid averaging peaks; subsample selects points
            self.curve.setDownsampling(1, True, mode='subsample')
        except Exception:
            pass

        layout.addWidget(self.plotw)

        # Pose bottom time label
        self.pose_time_label = QLabel("--:--:--", self)
        self.pose_time_label.setAlignment(Qt.AlignRight)
        self.pose_time_label.setStyleSheet("color: white; padding: 2px 4px;")
        layout.addWidget(self.pose_time_label)

        self._start_time: datetime | None = None
        self._min_index: int = 0
        self._last_x_range: tuple[int, int] | None = None

        # Unica linea cursore, riusata e aggiornata
        self._cursor_line = pg.InfiniteLine(angle=90, movable=False, pen=pg.mkPen('#88ffffff'))
        try:
            self.plotw.addItem(self._cursor_line)
        except Exception:
            pass

    def plot_example(self):
        self.curve.setData([], [])
        self.plotw.setTitle("", color='w')
        # Abilita auto-range solo su dataset piccolo di esempio
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

    def update_cursor(self, x_pos: float | int):
        try:
            self._cursor_line.setPos(float(x_pos))
        except Exception:
            pass

    # ==========================
    # Metodi helper statici (UI)
    # ==========================
    class TimeAxis(pg.AxisItem):
        def __init__(self, *args, formatter=None, **kwargs):
            super().__init__(*args, **kwargs)
            self._formatter = formatter
        def tickStrings(self, values, scale, spacing):
            try:
                if self._formatter:
                    return [self._formatter(v) for v in values]
            except Exception:
                pass
            return super().tickStrings(values, scale, spacing)

    @staticmethod
    def create_time_axis(formatter=None) -> "Graph.TimeAxis":
        """Crea un asse tempo personalizzato con un formatter esterno."""
        return Graph.TimeAxis(orientation='bottom', formatter=formatter)

    @staticmethod
    def _apply_perf_flags(item: pg.PlotDataItem):
        try:
            item.setClipToView(True)
            item.setAutoDownsample(True)
            item.setDownsampling(1, True, mode='subsample')
        except Exception:
            pass

    @staticmethod
    def add_curves(plot_item: pg.PlotItem, count: int, pens: list, show_legend: bool = True):
        """Aggiunge 1-3 curve al plot con performance flags e (opzionale) legenda.
        Ritorna la lista dei PlotDataItem creati.
        """
        items = []
        try:
            if show_legend:
                try:
                    plot_item.addLegend(offset=(-10, 5))
                except Exception:
                    pass
            names = ['X', 'Y', 'Z']
            for i in range(count):
                name = names[i] if count > 1 else None
                it = pg.PlotDataItem(pen=pens[i], name=name)
                Graph._apply_perf_flags(it)
                plot_item.addItem(it)
                items.append(it)
        except Exception:
            pass
        return items

    @staticmethod
    def add_vertical_cursors(plots: list[pg.PlotItem], pen: object):
        """Aggiunge una linea verticale non mobile ad ogni plot e la ritorna."""
        vlines = []
        try:
            for p in plots:
                ln = pg.InfiniteLine(angle=90, movable=False, pen=pen)
                p.addItem(ln)
                vlines.append(ln)
        except Exception:
            pass
        return vlines

    @staticmethod
    def build_multiplot_dashboard(
        pane_titles: list[str],
        bottom_formatter=None,
        right_panel=None
    ):
        """Crea un GraphicsLayoutWidget con una riga per titolo, linka gli assi X
        e inserisce nel right_panel se presente. Ritorna (glw, plots, bottom_plot, bottom_axis).
        """
        pg.setConfigOptions(antialias=True)
        glw = pg.GraphicsLayoutWidget(show=False)
        glw.ci.setContentsMargins(0, 0, 0, 0)
        glw.ci.layout.setSpacing(0)
        try:
            glw.ci.setBorder(None)
        except Exception:
            pass

        bottom_axis = Graph.create_time_axis(bottom_formatter)
        plots: list[pg.PlotItem] = []

        for row, title in enumerate(pane_titles):
            vb = NoRightZoomViewBox()
            vb.setDefaultPadding(0.0)
            if row == len(pane_titles) - 1:
                p = pg.PlotItem(viewBox=vb, axisItems={'bottom': bottom_axis})
            else:
                p = pg.PlotItem(viewBox=vb)

            try:
                p.layout.setContentsMargins(0, 0, 0, 0)
                p.setDefaultPadding(0.0)
            except Exception:
                pass

            p.setLabel('left', title, color='w')
            p.getAxis('left').setTextPen('w')
            p.getAxis('left').setPen('w')
            try:
                p.getAxis('left').setWidth(50)
            except Exception:
                pass

            p.setLimits(xMin=0.0, minXRange=300.0, maxXRange=300000.0)
            if row != len(pane_titles) - 1:
                p.getAxis('bottom').setStyle(showValues=False)
                p.getAxis('bottom').setHeight(0)
            else:
                p.getAxis('bottom').setTextPen('w')
                p.getAxis('bottom').setPen('w')

            p.showGrid(x=False, y=False, alpha=0.3)
            p.getViewBox().setMouseEnabled(x=True, y=False)

            try:
                legend = p.addLegend(offset=(-10, 5))
                legend.setBrush(pg.mkBrush(100, 100, 100, 150))
                legend.setLabelTextColor('w')
            except Exception:
                pass

            glw.addItem(p)
            if row < len(pane_titles) - 1:
                glw.nextRow()

            plots.append(p)

        # Link X tra tutti i plot
        bottom_plot = plots[-1]
        bottom_plot.getViewBox().setDefaultPadding(0.0)
        for p in plots[:-1]:
            p.getViewBox().setDefaultPadding(0.0)
            p.setXLink(bottom_plot)

        if right_panel is not None:
            try:
                layout = right_panel.layout()
                if hasattr(right_panel, "graph_frame") and right_panel.graph_frame is not None:
                    old = right_panel.graph_frame
                    try:
                        layout.replaceWidget(old, glw)
                        old.deleteLater()
                    except Exception:
                        # Fallback: inserisce al posto 0 se replace non funziona
                        try:
                            idx = layout.indexOf(old)
                            if idx < 0:
                                idx = 0
                            layout.insertWidget(idx, glw)
                            try:
                                layout.removeWidget(old)
                                old.setParent(None)
                            except Exception:
                                pass
                        except Exception:
                            layout.insertWidget(0, glw)
                else:
                    layout.insertWidget(0, glw)
                right_panel.graph_frame = glw
            except Exception:
                pass

        return glw, plots, bottom_plot, bottom_axis

    @staticmethod
    def connect_lod(bottom_plot: pg.PlotItem, target_pts: int = 2000):
        """Collega un handler per calcolare un passo di campionamento in base allo zoom."""
        def _apply_lod():
            try:
                vb = bottom_plot.getViewBox()
                x0, x1 = vb.viewRange()[0]
                _ = max(1, int((x1 - x0) / max(1.0, target_pts)))
                # Lo step è calcolato ma applicarlo è responsabilità del chiamante
                return _
            except Exception:
                return 1
        try:
            bottom_plot.getViewBox().sigXRangeChanged.connect(lambda *_: _apply_lod())
        except Exception:
            pass