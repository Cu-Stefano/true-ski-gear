from PySide6.QtWidgets import QWidget, QVBoxLayout, QPushButton
from PySide6.QtWebEngineWidgets import QWebEngineView

class Map(QWidget):
    
    def __init__(self, latitude=46.4983, longitude=11.3548, zoom=5, parent=None):
        super().__init__(parent)
        self.latitude = latitude
        self.longitude = longitude
        layout = QVBoxLayout(self)
        self.web_view = QWebEngineView(self)
        # Utilizza OpenStreetMap con layer satellitare (Esri World Imagery)
        html = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <title>Mappa Satellitare</title>
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <link rel="stylesheet" href="https://unpkg.com/leaflet/dist/leaflet.css" />
            <style>
                html, body, #map {{
                    height: 100%;
                    margin: 0;
                    padding: 0;
                }}
                .leaflet-control-attribution {{
                    display: none !important;
                }}
            </style>
        </head>
        <body>
            <div id="map"></div>
            <script src="https://unpkg.com/leaflet/dist/leaflet.js"></script>
            <script>
                var map = L.map('map').setView([{latitude}, {longitude}], {zoom});
                var marker = L.marker([{latitude}, {longitude}]).addTo(map);
                L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{{z}}/{{y}}/{{x}}', {{
                    attribution: 'Tiles &copy; Esri'
                }}).addTo(map);
            </script>
        </body>
        </html>
        """
        self.web_view.setHtml(html)
        # Pulsante per centrare
        self.center_button = QPushButton("Centra sul marker", self)
        self.center_button.clicked.connect(self.center_map)
        layout.addWidget(self.center_button)
        layout.addWidget(self.web_view)
        self.setLayout(layout)
        self.web_view.loadFinished.connect(self._page_ready)

    def _page_ready(self):
        pass  # placeholder se servono azioni future

    def center_map(self):
        js = f"map.setView([{self.latitude}, {self.longitude}], map.getZoom());"
        self.web_view.page().runJavaScript(js)
        
    def MoveMarker(self, latitude, longitude):
        self.latitude = latitude
        self.longitude = longitude
        js = f"""
        marker.setLatLng([{self.latitude}, {self.longitude}]);
        map.setView([{self.latitude}, {self.longitude}], map.getZoom());
        """
        self.web_view.page().runJavaScript(js)