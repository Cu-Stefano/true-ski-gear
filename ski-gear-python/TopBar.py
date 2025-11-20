from PySide6.QtWidgets import (
    QWidget, QLabel, QPushButton, QHBoxLayout, QComboBox
)
import Utilities

class TopBar(QWidget):
    
    def on_update_ports(self):
        print("Update ports")
    def on_connect(self):
        print("Connect")
    def on_disconnect(self):
        print("Disconnect")
        
    def __init__(self, parent):
        super().__init__()
        layout = QHBoxLayout(self)
        
        self.update_ports = Utilities.createButton("Update ports", self.on_update_ports)
        self.port_select = QComboBox()
        self.connect_button = Utilities.createButton("Connect", self.on_connect)
        self.disconnect_button = Utilities.createButton("Disconnect", self.on_disconnect)
        
        for w in [self.update_ports, self.port_select, self.connect_button, self.disconnect_button]:
            layout.addWidget(w)
        layout.addStretch(1)
        self.setLayout(layout)

