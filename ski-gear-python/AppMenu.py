from PySide6.QtGui import QAction
from PySide6.QtWidgets import QMainWindow, QMenuBar
from PySide6.QtWidgets import QFileDialog
import Utilities
import SessionV2

class AppMenu:
    def __init__(self, main_window, left_panel, right_panel):
        self.left_panel = left_panel
        self.right_panel = right_panel
        self.main_window = main_window
        self.menu = self.main_window.menuBar()
        self.create_menu()

    def Actions_button_clicked(self):
        print("Actions button clicked")

    def advanced_button_clicked(self):
        print("advanced button clicked")

    def load_button_clicked(self):
        file_dialog = QFileDialog(self.main_window)
        file_dialog.setNameFilter("Track file (*.session *.track *.dat)")
        file_dialog.setFileMode(QFileDialog.FileMode.ExistingFile)
        if file_dialog.exec():
            selected_file = file_dialog.selectedFiles()[0]
            print(f"Selected file: {selected_file}")
            deviceID = 0
            sessionID = 0
            sV2 = SessionV2.SessionV2(deviceID, sessionID, selected_file, self.right_panel)
            sV2.ReadSessionFromFileV2(selected_file)
            # for i in range(3):
            # sV2.get_main_data(0, 0)
            
            

    def create_menu(self):
        export_action = Utilities.createAction("&Export1", self.Actions_button_clicked, self.main_window, "export stuff", False)
        actions_action = Utilities.createAction("&Action1", self.Actions_button_clicked, self.main_window, "actons", False)
        advanced_action = Utilities.createAction("&Advanced", self.advanced_button_clicked, self.main_window, "advanced options", False)
        load_action = Utilities.createAction("&Load track from file", self.load_button_clicked, self.main_window, "Load stuff", False)

        load_menu = self.menu.addMenu("Load track from file")
        load_menu.addAction(load_action)
        load_menu.addAction(export_action)

        actions_menu = self.menu.addMenu("Actions")
        actions_menu.addAction(actions_action)

        advanced_menu = self.menu.addMenu("Advanced")
        advanced_menu.addAction(advanced_action)