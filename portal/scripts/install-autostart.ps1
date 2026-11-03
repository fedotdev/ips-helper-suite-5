<#
.SYNOPSIS
    Ставит автозапуск портала и общего сервера OpenCode через Планировщик задач Windows.

.DESCRIPTION
    Создаёт две задачи:
      1) IPS-OpenCode-Service - поднимает общий фоновый сервер OpenCode
                                (opencode service start);
      2) IPS-Portal           - запускает портал (node src/index.ts).

    Секреты в репозиторий не попадают: пароль сервера OpenCode берётся из
    переменной окружения OPENCODE_SERVER_PASSWORD либо из файла, указанного в
    opencode.passwordFile (в том числе из самого service.json OpenCode).

.PARAMETER Mode
    Logon   - задачи запускаются при входе пользователя в Windows (по умолчанию,
              права администратора не нужны).
    Startup - задачи запускаются при старте Windows (нужны права администратора).

.PARAMETER PortalPort
    Порт портала (по умолчанию 8099).

.PARAMETER NodePath
    Путь к node.exe. По умолчанию берётся из PATH.

.PARAMETER OpenCodeUrl
    Если задан - портал будет ходить по этому адресу вместо автоопределения
    (opencode service status).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\install-autostart.ps1
#>

[CmdletBinding()]
param(
  [ValidateSet("Logon", "Startup")]
  [string]$Mode = "Logon",
  [int]$PortalPort = 8099,
  [string]$NodePath = "",
  [string]$OpenCodeUrl = ""
)

$ErrorActionPreference = "Stop"

$PortalDir = Split-Path -Parent $PSScriptRoot
$ProjectDir = Split-Path -Parent $PortalDir

function Write-Step([string]$Text) {
  Write-Host "==> $Text" -ForegroundColor Cyan
}

if (-not $NodePath) {
  $cmd = Get-Command node -ErrorAction SilentlyContinue
  if (-not $cmd) { throw "node не найден в PATH. Укажите -NodePath." }
  $NodePath = $cmd.Source
}
Write-Step "node: $NodePath"

# --- конфигурация -----------------------------------------------------------

$ConfigPath = Join-Path $PortalDir "config.json"
if (-not (Test-Path $ConfigPath)) {
  $example = Join-Path $PortalDir "config.example.json"
  if (-not (Test-Path $example)) { throw "Нет ни config.json, ни config.example.json" }
  Copy-Item $example $ConfigPath
  Write-Step "Создан config.json из config.example.json - проверьте его"
}

# Адрес сервера OpenCode: либо фиксированный, либо автоопределение.
if ($OpenCodeUrl) {
  $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
  $config.opencode.baseUrl = $OpenCodeUrl
  $config.server.port = $PortalPort
  $config | ConvertTo-Json -Depth 10 | Set-Content $ConfigPath -Encoding UTF8
  Write-Step "В config.json записан opencode.baseUrl = $OpenCodeUrl"
} elseif ($env:OPENCODE_SERVER_PASSWORD) {
  Write-Step "Пароль OpenCode берётся из переменной окружения OPENCODE_SERVER_PASSWORD"
} else {
  $serviceJson = Join-Path $env:USERPROFILE ".config\opencode\service.json"
  if (Test-Path $serviceJson) {
    $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
    $config.opencode.passwordFile = $serviceJson
    $config.opencode.baseUrl = "auto"
    $config.server.port = $PortalPort
    $config | ConvertTo-Json -Depth 10 | Set-Content $ConfigPath -Encoding UTF8
    Write-Step "В config.json указан opencode.passwordFile = $serviceJson"
  } else {
    throw "Нет ни OPENCODE_SERVER_PASSWORD, ни файла service.json. Задайте пароль вручную."
  }
}

# --- задачи Планировщика ----------------------------------------------------

$Trigger = if ($Mode -eq "Startup") {
  New-ScheduledTaskTrigger -AtStartup
} else {
  New-ScheduledTaskTrigger -AtLogOn
}

$settings = New-ScheduledTaskSettingsSet `
  -AllowStartIfOnBatteries `
  -DontStopIfGoingOnBatteries `
  -StartWhenAvailable `
  -ExecutionTimeLimit ([TimeSpan]::Zero) `
  -RestartCount 5 `
  -RestartInterval (New-TimeSpan -Minutes 1)

# 1. Общий сервер OpenCode.
$ocAction = New-ScheduledTaskAction -Execute "opencode.cmd" -Argument "service start" -WorkingDirectory $ProjectDir
$ocTask = New-ScheduledTask -Action $ocAction -Trigger $Trigger -Settings $settings `
  -Description "Поднимает общий фоновый сервер OpenCode для портала IPS"

# 2. Портал.
$portalArgs = "`"$($PortalDir)\src\index.ts`" --config `"$ConfigPath`" --port $PortalPort"
$portalAction = New-ScheduledTaskAction -Execute $NodePath -Argument $portalArgs -WorkingDirectory $PortalDir
$portalTask = New-ScheduledTask -Action $portalAction -Trigger $Trigger -Settings $settings `
  -Description "Веб-портал IPS поверх общего экземпляра opencode serve"

foreach ($pair in @(@("IPS-OpenCode-Service", $ocTask), @("IPS-Portal", $portalTask))) {
  $name = $pair[0]
  $task = $pair[1]
  Unregister-ScheduledTask -TaskName $name -Confirm:$false -ErrorAction SilentlyContinue
  Register-ScheduledTask -TaskName $name -TaskPath "\" -InputObject $task -Force | Out-Null
  Write-Step "Задача '$name' зарегистрирована (режим: $Mode)"
}

Write-Step "Готово. Запуск вручную:"
Write-Host "    Start-ScheduledTask -TaskName 'IPS-OpenCode-Service'"
Write-Host "    Start-ScheduledTask -TaskName 'IPS-Portal'"
Write-Host "    Открыть: http://127.0.0.1:$PortalPort"
