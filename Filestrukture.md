# Filestrukture.md

GatewayIDE/
├── GatewayIDE.App/
│   ├── Services/
│   │   ├── App/
│   │   │   ├── Appstate.cs
│   │   │   ├── GatewayIDEConfig C#
│   │   │   ├── NetworkRegistryModels C#
│   │   │   ├── RegistryService C#
│   │   │   ├── ServiceRegistryModels C#
│   │   │   └── SettingsService C#
│   │   ├── Auth/
│   │   │   └── AuthBootstrapService C#
│   │   ├── Chat/
│   │   │   └── ChatService C#
│   │   ├── Docker/
│   │   │   └── DockerService C#
│   │   └── Network/
│   │       ├── GithubDeviceFlow C#
│   │       ├── NetworkApiService C#
│   │       ├── NetworkDtos C#
│   │       ├── NetworkHost C#
│   │       ├── NetworkSession C#
│   │       └── WireGuardKeys C#
│   ├── Views/
│   │   ├── Chat/
│   │   │   ├── SidePanel.axaml
│   │   │   ├── SidePanel C#
│   │   │   ├── SidePanelCommands C#
│   │   │   ├── SidePanelState C#
│   │   │   └── ChatState C#
│   │   ├── Dashboard/
│   │   │   ├── DashboardPanel.axaml
│   │   │   ├── DashboardPanel C#
│   │   │   ├── DashboardPanelState C#
│   │   │   └── DashboardPanelCommands C#
│   │   ├── Docker/
│   │   │   ├── DockerPanel C#
│   │   │   ├── DockerPanelState C#
│   │   │   ├── DockerPanelCommands C#
│   │   │   ├── DockerPanel.axaml
│   │   │   ├── ServiceUnitVm C#
│   │   │   ├── UnitConfig C#
│   │   │   ├── UnitRuntime C#
│   │   │   └── UnitStatus C#
│   │   ├── Engines/
│   │   │   ├── EnginesPanel.axaml
│   │   │   ├── EnginesPanel C#
│   │   │   ├── EnginesPanelCommands C#
│   │   │   └── EnginesPanelState C#
│   │   ├── Explorer/
│   │   │   ├── ExplorerPanel.axaml
│   │   │   ├── ExplorerPanel C#
│   │   │   ├── ExplorerPanelState C#
│   │   │   └── ExplorerPanelCommands C#
│   │   ├── GitHub/
│   │   │   ├── GitHubPanel.axaml
│   │   │   ├── GitHubPanel C#
│   │   │   ├── GitHubPanelState C#
│   │   │   └── GitHubPanelCommands C#
│   │   ├── KiSystem/
│   │   │   ├── KiSystemPanel.axaml
│   │   │   ├── KiSystemPanel C#
│   │   │   ├── KiSystemPanelState C#
│   │   │   ├── KiSystemPanelCommands C#
│   │   │   └── ThreadRouter C#
│   │   ├── Network/
│   │   │   ├── NetworkPanel.axaml 
│   │   │   ├── NetworkPanel C#
│   │   │   ├── NetworkPanelState C#
│   │   │   └── NetworkPanelCommands C#
│   │   ├── Settings/
│   │   │   ├── SettingsPanel.axaml
│   │   │   ├── SettingsPanel C#
│   │   │   ├── SettingsPanelState C#
│   │   │   └── SettingsPanelCommands C#
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
└── uv.lock


