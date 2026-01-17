param(
  [ValidateSet("check","install-git","install-docker")]
  [string]$Action = "check"
)

$ErrorActionPreference = "SilentlyContinue"

function Has-Command($name) {
  $cmd = Get-Command $name -ErrorAction SilentlyContinue
  return $null -ne $cmd
}

function Has-Winget {
  return Has-Command "winget"
}

function Write-Result([bool]$ok, [string]$label) {
  if ($ok) { Write-Host "[OK]  $label" }
  else     { Write-Host "[MISS] $label" }
}

function Check-All {
  $hasGit = Has-Command "git"
  $hasDockerCli = Has-Command "docker"

  # Docker Desktop kann installiert sein, aber docker.exe nicht im PATH sein.
  # Deshalb zusätzlich grob über Registry prüfen (nur Indikator).
  $dockerDesktopReg = Get-ItemProperty "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*" `
    | Where-Object { $_.DisplayName -like "*Docker Desktop*" } | Select-Object -First 1
  $hasDockerDesktop = $null -ne $dockerDesktopReg

  Write-Result $hasGit "Git"
  if ($hasDockerCli -or $hasDockerDesktop) {
    Write-Host "[OK]  Docker"
  } else {
    Write-Host "[MISS] Docker"
  }

  # Exitcodes: 0 = alles da, 1 = etwas fehlt
  if ($hasGit -and ($hasDockerCli -or $hasDockerDesktop)) { exit 0 } else { exit 1 }
}

function Install-Git {
  if (Has-Command "git") { Write-Host "[OK] Git ist bereits vorhanden"; exit 0 }

  if (Has-Winget) {
    Write-Host "[RUN] winget install Git.Git"
    winget install --id Git.Git -e --source winget
    exit $LASTEXITCODE
  } else {
    Write-Host "[OPEN] winget fehlt -> öffne Downloadseite"
    Start-Process "https://git-scm.com/download/win"
    exit 1
  }
}

function Install-Docker {
  # Docker Desktop braucht Admin und evtl. WSL2, Neustart etc.
  if (Has-Winget) {
    Write-Host "[RUN] winget install Docker.DockerDesktop"
    winget install --id Docker.DockerDesktop -e --source winget
    exit $LASTEXITCODE
  } else {
    Write-Host "[OPEN] winget fehlt -> öffne Downloadseite"
    Start-Process "https://www.docker.com/products/docker-desktop/"
    exit 1
  }
}

switch ($Action) {
  "check"         { Check-All }
  "install-git"   { Install-Git }
  "install-docker"{ Install-Docker }
}
