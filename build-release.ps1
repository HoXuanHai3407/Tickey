param(
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'StickyNotePremium.csproj'
$output = Join-Path $PSScriptRoot 'publish\win-x64'
$selfContainedValue = if ($SelfContained) { 'true' } else { 'false' }

Write-Host "Publishing Sticky Note Premium -> $output"
Write-Host "Self-contained: $selfContainedValue"

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained $selfContainedValue `
    -o $output `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

Write-Host "Done: $output"
