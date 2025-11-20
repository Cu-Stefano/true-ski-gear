from PySide6.QtWidgets import QPushButton
from PySide6.QtGui import QAction

def createButton(text, callback):
    button = QPushButton(text)
    button.clicked.connect(callback)
    return button

def createAction(text, callback, parent=None, statusTip="", checkable=False):
    action = QAction(text, parent)
    action.setStatusTip(statusTip)
    action.setCheckable(checkable)
    action.triggered.connect(callback)
    return action