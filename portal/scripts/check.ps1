<#
.SYNOPSIS
    Диагностика: проверяет, что сервер OpenCode отвечает и портал запускается.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\check.ps1
#>

[CmdletBinding()]
param(
  [string]$ConfigPath = ""
)

$ErrorActionPreference = "Continue"

$PortalDir = Split-Path -Parent $PSScriptRoot
if (-not $ConfigPath) { $ConfigPath = Join-Path $PortalDir "config.json" }

Write-Host "==> OpenCode" -ForegroundColor Cyan
$status = opencode service status 2>&1
Write-Host "    service status: $status"

$url = $null
if ($status -match 'https?://[^\s]+') { $url = $Matches[0] }
if (-not $url) {
  Write-Host "    ВНИМАНИЕ: не удалось определить адрес сервера OpenCode" -ForegroundColor Red
} else {
  Write-Host "    URL: $url"
  try {
    $cred = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("opencode:" + $env:OPENCODE_SERVER_PASSWORD))
    $agents = Invoke-RestMethod -Uri "$url/api/agent" -Headers @{ Authorization = "Basic $cred" } -TimeoutSec 10
    Write-Host "    Агентов: $(($agents.data | Measure-Object).Count)" -ForegroundColor Green
  } catch {
    Write-Host "    ВНИМАНИЕ: не удалось получить список агентов: $($_.Exception.Message)" -ForegroundColor Red
  }
}

Write-Host "==> Портал" -ForegroundColor Cyan
if (-not (Test-Path $ConfigPath)) {
  Write-Host "    Нет $ConfigPath" -ForegroundColor Red
  exit 1
}
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
Write-Host "    Каталог проекта: $($config.opencode.projectDirectory)"
Write-Host "    Каталог результатов: $($config.output.root)"
Write-Host "    Модели: $($config.allowed_models -join ', ')"

$port = $config.server.port
try {
  $r = Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/agents" -Headers @{ "X-User" = "diagnostic" } -TimeoutSec 5 -UseBasicParsing
  Write-Host "    Портал отвечает: HTTP $($r.StatusCode)" -ForegroundColor Green
} catch {
  Write-Host "    Портал не отвечает на порту $port : $($_.Exception.Message)" -ForegroundColor Red
}
