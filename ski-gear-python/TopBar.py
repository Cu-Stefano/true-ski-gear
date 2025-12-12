from PySide6.QtWidgets import (
    QWidget, QLabel, QPushButton, QHBoxLayout, QComboBox
)
from PySide6.QtCore import Signal
import logging
import Utilities

class TopBar(QWidget):
    updatePortsRequested = Signal()
    connectRequested = Signal(str) 
    disconnectRequested = Signal()
    
    def on_update_ports(self):
        logging.getLogger(__name__).info("Update ports requested")
        self.updatePortsRequested.emit()
        
    def on_connect(self):
        logging.getLogger(__name__).info("Connect requested")
        self.connectRequested.emit(self.port_select.currentText())
        
    def on_disconnect(self):
        logging.getLogger(__name__).info("Disconnect requested")
        self.disconnectRequested.emit()
        
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

