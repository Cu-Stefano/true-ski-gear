import sys
from PySide6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout,
    QHBoxLayout
)
import TopBar
import LeftPanel
import RightPanel
import AppMenu
import matplotlib.pyplot as plt

class MainWindow(QMainWindow):
    
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Track Analyzer")
        self.setMinimumSize(600, 600)
        self.resize(1600, 800)

        main_widget = QWidget()
        main_layout = QVBoxLayout(main_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)

        # Top bar
        self.top_bar = TopBar.TopBar(self)
        main_layout.addWidget(self.top_bar)

        # Main center layout
        center_layout = QHBoxLayout()
        center_layout.setContentsMargins(0, 0, 0, 0)
        center_layout.setSpacing(0)
        
        self.left_panel = LeftPanel.LeftPanel()
        self.right_panel = RightPanel.RightPanel(self, self.left_panel.map_widget)
        
        # Top Menu toolBar
        self.app_menu = AppMenu.AppMenu(self, self.left_panel, self.right_panel)
        
        center_layout.addWidget(self.left_panel, 1)
        center_layout.addWidget(self.right_panel, 2)
        main_layout.addLayout(center_layout)

        self.setCentralWidget(main_widget)

 
if __name__ == "__main__":
    app = QApplication(sys.argv)
    w = MainWindow()
    # --- Modifica il grafico di RightPanel dall'esterno (qui in main) ---
    import numpy as np
    t = np.linspace(0, 10, 500)
    y1 = np.sin(t) * 50 + 100
    y2 = np.cos(t) * 20
    w.right_panel.set_graph_data(t, [y1, y2],
                                 colors=["#ff3333", "#3388ff"],
                                 labels=["sin", "cos"],
                                 title="Demo (aggiornato da main)")
    # ---------------------------------------------------------------
    
    w.show()
    sys.exit(app.exec())
