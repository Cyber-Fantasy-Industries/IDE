## IDE

1. **Avalonia-Desktop-App** – Benutzeroberfläche mit integriertem Terminal 

3. **Docker-Compose-Umgebung** – Einheitliche Laufzeitumgebung für Backend, Speicher und optionale Services.


### Features
  * Entwickelt mit Avalonia (.NET 8, MVVM-Struktur)


#### Voraussetzungen
* **.NET 8 SDK** (für das Avalonia-Frontend)
* **Python ≥ 3.10** mit den Paketen aus `pyproject.toml`
* **Docker Desktop** mit Compose v2



### Windows-Build
Das Script `build-win.bat` erstellt einen self-contained Release-Build in `dist/win-x64`. Beispiel:




IDE by Aaron Lindsay