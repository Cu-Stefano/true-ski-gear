from PySide6.QtGui import QAction
from PySide6.QtWidgets import QMainWindow, QMenuBar
import Utilities

class AppMenu:
    
    def Actions_button_clicked(self):
        print("Actions button clicked")
    def advanced_button_clicked(self):
        print("advanced button clicked")
    def load_button_clicked(self):
        print("load button clicked")
        
    def __init__(self, main_window):
        self.main_window = main_window
        self.menu = self.main_window.menuBar()
        self.create_menu()

    def create_menu(self):
        export_action = Utilities.createAction("&Export1", self.Actions_button_clicked, self.main_window, "export stuff", False)
        actions_action = Utilities.createAction("&Action1", self.Actions_button_clicked, self.main_window, "actons", False)
        advanced_action = Utilities.createAction("&Advanced", self.advanced_button_clicked, self.main_window, "advanced options", False)
        load_action = Utilities.createAction("&Load", self.load_button_clicked, self.main_window, "Load stuff", False)

        load_menu = self.menu.addMenu("Load track from file")
        load_menu.addAction(load_action)
        load_menu.addAction(export_action)

        actions_menu = self.menu.addMenu("Actions")
        actions_menu.addAction(actions_action)

        advanced_menu = self.menu.addMenu("Advanced")
        advanced_menu.addAction(advanced_action)