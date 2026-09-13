<#
.SYNOPSIS
Запуск C4ModelBuilder с использованием относительных путей.

.DESCRIPTION
Скрипт предназначен для запуска из корневой папки репозитория.
#>

[CmdletBinding()]
param(
    [string] $SolutionPath = 'C4ModelBuilder\C4ModelBuilder.sln',
    [string] $OutputPath = 'output',
    [int] $MaxDepth = 16,
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

# Относительный путь к CLI-проекту от корня репозитория
$cliProjectPath = 'C4ModelBuilder\C4ModelBuilder.Cli\C4ModelBuilder.Cli.csproj'

if (-not (Test-Path $SolutionPath)) {
    Write-Error "Файл решения не найден по пути '$SolutionPath'. Убедитесь, что скрипт запущен из корневой папки репозитория."
    exit 1
}

Write-Host 'Запуск C4ModelBuilder...' -ForegroundColor Cyan
Write-Host "  Solution:  $SolutionPath"
Write-Host "  Output:    $OutputPath"
Write-Host "  MaxDepth:  $MaxDepth"

& dotnet run `
    --project $cliProjectPath `
    --configuration $Configuration `
    -- `
    -s $SolutionPath `
    -o $OutputPath `
    -d $MaxDepth

exit $LASTEXITCODE