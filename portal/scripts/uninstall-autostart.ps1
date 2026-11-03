<#
.SYNOPSIS
    Снимает автозапуск портала (задачи Планировщика Windows).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\uninstall-autostart.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

foreach ($name in @("IPS-Portal", "IPS-OpenCode-Service")) {
  $task = Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
  if ($task) {
    Unregister-ScheduledTask -TaskName $name -Confirm:$false
    Write-Host "Задача '$name' удалена" -ForegroundColor Yellow
  } else {
    Write-Host "Задача '$name' не найдена" -ForegroundColor DarkGray
  }
}
