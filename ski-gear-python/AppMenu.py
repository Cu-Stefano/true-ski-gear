from PySide6.QtGui import QAction
from PySide6.QtWidgets import QMainWindow, QMenuBar
from PySide6.QtWidgets import QFileDialog
import Utilities
import SessionV2
import pyqtgraph as pg

class AppMenu:
    def __init__(self, main_window, left_panel, right_panel):
        self.left_panel = left_panel
        self.right_panel = right_panel
        self.main_window = main_window
        self.menu = self.main_window.menuBar()
        self.create_menu()
        self.series = [[0 for _ in range(3)] for _ in range(9)]

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
            deviceID = 0
            sessionID = 0

            sV2 = SessionV2.SessionV2(deviceID, sessionID, selected_file, self.right_panel)
            # Carica nel DB
            sV2.ReadSessionFromFileV2(selected_file)
            sV2.InitSessionPlotModel(self.series, axis=2)

            min_idx = int(sV2.MinIndex)
            max_idx = int(sV2.MaxIndex)

            def _set_xyz(bucket_idx: int, getter):
                for ax in (0, 1, 2):
                    xs, ys = getter(ax)
                    if xs is not None and getattr(xs, "size", len(xs)) > 0:
                        self.series[bucket_idx][ax].setData(xs, ys, connect='finite')

            _set_xyz(0, lambda ax: sV2.get_fast_acc_series(min_idx, max_idx, ax))

            for b in range(1, 5):
                idx_slow = b - 1 
                _set_xyz(b, lambda ax, i=idx_slow: sV2.get_slow_acc_series(i, min_idx, max_idx, ax))

            _set_xyz(sV2.gyro_index, lambda ax: sV2.get_gyro_series(min_idx, max_idx, ax))

            _set_xyz(7, lambda ax: sV2.get_pose_series(min_idx, max_idx, ax))

            for ax in (0, 1, 2):
                xs_g, ys_g = sV2.get_gravity_series(min_idx, max_idx, axis=ax)
                if xs_g is not None and getattr(xs_g, "size", len(xs_g)) > 0:
                    self.series[8][ax].setData(xs_g, ys_g, connect='finite')

            xs_sp, ys_sp = sV2.get_speed_series(min_idx, max_idx)
            if xs_sp is not None and getattr(xs_sp, "size", len(xs_sp)) > 0:
                self.series[sV2.speed_index][0].setData(xs_sp, ys_sp, connect='finite')
            
            try:
                sV2.apply_y_ticks()
            except Exception:
                pass
            

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