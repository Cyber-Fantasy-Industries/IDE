# Filestrukture.md

GatewayIDE/
├── GatewayIDE.App/
│   ├── Services/
│   │   ├── App/
│   │   │   ├── GatewayIDEConfig C#
│   │   │   ├── SettingsService C#
│   │   │   ├── RegistryService C#
│   │   │   ├── ServiceRegistryModels C#
│   │   │   ├── NetworkRegistryModels C#
│   │   │   └── Appstate.cs
│   │   ├── Auth/
│   │   │   └── AuthBootstrapService C#
│   │   ├── Chat/
│   │   │   └── ChatService C#
│   │   ├── Docker/
│   │   │   └── DockerService C#
│   │   └── Network/
│   │       ├── GithubDeviceFlow C#
│   │       ├── WireGuardKeys C#
│   │       ├── NetworkApiService C#
│   │       ├── NetworkSession C#
│   │       └── NetworkDtos C#
│   ├── Views/
│   │   ├── Chat/
│   │   │   ├── SidePanel.axaml
│   │   │   ├── SidePanel C#
│   │   │   └── ChatState C#
│   │   ├── Dashboard/
│   │   │   ├── DashboardPanel.axaml
│   │   │   ├── DashboardPanel C#
│   │   ├── Docker/
│   │   │   ├── DockerController C#
│   │   │   ├── DockerLogs C#
│   │   │   ├── DockerPanel.axaml
│   │   │   ├── DockerPanel C#
│   │   │   ├── DockerState C#
│   │   │   ├── DockerUi C#
│   │   │   ├── DockerUnitsCatalog C#
│   │   │   ├── ServiceUnitVm C#
│   │   │   ├── UnitConfig C#
│   │   │   ├── UnitRuntime C#
│   │   │   └── UnitStatus C#
│   │   ├── Engines/
│   │   │   ├── EnginesPanel.axaml
│   │   │   ├── EnginesPanel C#
│   │   ├── Explorer/
│   │   │   ├── ExplorerPanel.axaml
│   │   │   ├── ExplorerPanel C#
│   │   │   ├── SettingsmPanel C#
│   │   ├── GitHub/
│   │   │   ├── GitHubPanel.axaml
│   │   │   ├── GitHubPanel C#
│   │   ├── KiSystem/
│   │   │   ├── KiSystemPanel.axaml
│   │   │   ├── KiSystemPanel C#
│   │   │   └── ThreadRouter C#
│   │   ├── Network/
│   │   │   ├── NetworkPanel.axaml 
│   │   │   └── NetworkPanel C#
│   │   ├── Settings/
│   │   │   ├── SettingsPanel.axaml
│   │   │   └── SettingsPanel C#
│   │   ├── LeftRail.axaml
│   │   ├── LeftRail C#
│   │   ├── LayoutState C#
│   │   ├── Layout.axaml
│   │   └── Layout C#
│   ├── GatewayIDE.App C#
│   ├── App.axaml
│   ├── App C#
│   ├── AppBootstrap C#
│   ├── MainWindow.axaml
│   ├── MainWindow C#
│   ├── Commands C#
│   ├── Converters C#
│   ├── ViewModelBase C#
│   ├── MainState C#
│   └── Program C#
├── Network/
│   ├── __init__
│   ├── main.py
│   ├── bootstrap.py
│   ├── pyproject.toml
│   ├── uv.lock
│   ├── network/
│   │   ├── __init__
│   │   ├── models.py
│   │   ├── nebula.py
│   │   ├── README
│   │   ├── root
│   │   ├── store.py
│   │   └── wireguard.py
│   └── routes/
│       ├── __init__
│       ├── network.py
│       └── admin_network.py
├── .dockerignore
├── .gitattributes
├── .gitignore
├── build-win.bat
├── dev.env
├── net.dev.env
├── net.prod.env
├── docker-compose.yml
├── Dockerfile.network
├── GatewayIDE.sln
├── pyproject.toml
├── README.md
├── AppBootstrap.cs
└── uv.lock


