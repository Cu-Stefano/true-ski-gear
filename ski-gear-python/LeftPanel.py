from PySide6.QtWidgets import (
    QWidget, QLabel, QPushButton, QVBoxLayout,
    QHBoxLayout, QListWidget, QDateTimeEdit, QSizePolicy
)
from PySide6.QtCore import Qt, QTimer
import Map
import Utilities

class LeftPanel(QWidget):
    
    def on_mark_tagging_completed(self):
        print("Mark tagging as completed")
    def on_save_track_on_file(self):
        print("Save track on file")
    def on_high_side(self):
        print("High Side")
    def on_low_side(self):
        print("Low Side")
    def on_other(self):
        print("Other")
    
    def __init__(self):
        super().__init__()
        layout = QVBoxLayout(self)
        
        # Map widget
        self.map_widget = Map.Map(latitude=46.4983, longitude=11.3548, zoom=16)
        self.map_widget.setMinimumHeight(250)
        layout.addWidget(self.map_widget)
        QTimer.singleShot(0, self._update_map_max_height)

        btn_row = QHBoxLayout()
        self.mark_tagging_btn = Utilities.createButton("Mark tagging as completed", self.on_mark_tagging_completed)
        self.save_track_btn = Utilities.createButton("Save track on file", self.on_save_track_on_file)
        btn_row.addWidget(self.mark_tagging_btn)
        btn_row.addWidget(self.save_track_btn)
        layout.addLayout(btn_row)

        time_row = QHBoxLayout()
        time_row.addWidget(QLabel("Data:"))
        self.date_edit = QDateTimeEdit()
        self.date_edit.setCalendarPopup(True)
        time_row.addWidget(self.date_edit)
        layout.addLayout(time_row)

        tag_row = QHBoxLayout()
        self.high_side_btn = Utilities.createButton("High Side", self.on_high_side)
        self.low_side_btn = Utilities.createButton("Low Side", self.on_low_side)
        self.other_btn = Utilities.createButton("Other", self.on_other)
        
        tag_row.addWidget(self.high_side_btn)
        tag_row.addWidget(self.low_side_btn)
        tag_row.addWidget(self.other_btn)
        layout.addLayout(tag_row)

        self.events_list = QListWidget()
        layout.addWidget(self.events_list)
        self.setLayout(layout)

    def showEvent(self, event):
        super().showEvent(event)
        self._update_map_max_height()

    def resizeEvent(self, event):
        super().resizeEvent(event)
        self._update_map_max_height()

    def _update_map_max_height(self):
        win = self.window()
        if win and win.height() > 0:
            target = max(300, int(win.height() * 0.5))
            self.map_widget.setFixedHeight(target)