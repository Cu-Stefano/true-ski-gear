import sys
import numpy as np
from PySide6.QtWidgets import QApplication, QMainWindow, QWidget, QVBoxLayout
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg
from matplotlib.figure import Figure
from typing import Iterable, Sequence


class Graph(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)

        # Layout verticale
        layout = QVBoxLayout(self)

        # Figura Matplotlib
        self.figure = Figure(figsize=(5, 3))
        self.canvas = FigureCanvasQTAgg(self.figure)

        # Aggiunge canvas al widget
        layout.addWidget(self.canvas)

        # Asse
        self.ax = self.figure.add_subplot(111)


    def plot_example(self):
        t = np.linspace(0, 10, 500)
        y1 = np.sin(t) * 50 + 100
        y2 = np.random.normal(0, 5, size=500)
        y3 = np.cos(t) * 20

        self.ax.clear()

        self.ax.plot(t, y1, color="red")
        self.ax.plot(t, y2, color="blue")
        self.ax.plot(t, y3, color="green")

        self.ax.set_title("Grafico in PySide6")
        self.ax.grid(True)

        self.canvas.draw()

    def set_data(self, x: Iterable[float], y_list: Sequence[Iterable[float]],
                 colors: Sequence[str] | None = None,
                 labels: Sequence[str] | None = None,
                 title: str | None = None) -> None:
        """Aggiorna il grafico dall'esterno."""
        self.ax.clear()
        for i, y in enumerate(y_list):
            color = (colors[i] if colors and i < len(colors) else None)
            label = (labels[i] if labels and i < len(labels) else None)
            self.ax.plot(list(x), list(y), color=color, label=label)
        if labels:
            self.ax.legend(loc="upper right")
        if title:
            self.ax.set_title(title)
        self.ax.grid(True)
        self.canvas.draw_idle()


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()

        self.setWindowTitle("Matplotlib in PySide6")

        # Crea widget grafico
        self.graph = Graph()
        self.setCentralWidget(self.graph)

        # Disegna dati di prova
        self.graph.plot_example()


if __name__ == "__main__":
    app = QApplication(sys.argv)

    w = MainWindow()
    w.resize(900, 500)
    w.show()

    sys.exit(app.exec())
