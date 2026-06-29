param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir,
    [Parameter(Mandatory = $true)]
    [string]$TargetDir,
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'

$pluginRoot  = Join-Path $WorkspaceRoot 'plugin'
$releaseRoot = Join-Path $WorkspaceRoot "release\v$Version"
$pluginName  = 'RetakeSpawnEditor'

$pkgWithConfigs = Join-Path $pluginRoot "pkg-with-configs\addons\counterstrikesharp\plugins\$pluginName"
$pkgNoConfigs   = Join-Path $pluginRoot "pkg-no-configs\addons\counterstrikesharp\plugins\$pluginName"
$cfgWithConfigs = Join-Path $pluginRoot "pkg-with-configs\addons\counterstrikesharp\configs\plugins\$pluginName"
$cfgBase        = Join-Path $pluginRoot "config-base\addons\counterstrikesharp\configs\plugins\$pluginName"

$templateCfgWithConfigs = Join-Path $releaseRoot "pkg-with-configs\addons\counterstrikesharp\configs\plugins\$pluginName"
$templateCfgBase        = Join-Path $releaseRoot "config-base\addons\counterstrikesharp\configs\plugins\$pluginName"

foreach ($path in @($pkgWithConfigs, $pkgNoConfigs, $cfgWithConfigs, $cfgBase)) {
    if (Test-Path $path) { Remove-Item -Path $path -Recurse -Force }
    New-Item -Path $path -ItemType Directory -Force | Out-Null
}

Copy-Item -Path (Join-Path $TargetDir '*') -Destination $pkgWithConfigs -Recurse -Force
Copy-Item -Path (Join-Path $TargetDir '*') -Destination $pkgNoConfigs   -Recurse -Force

if (-not (Test-Path $templateCfgWithConfigs)) { throw "Template manquant : $templateCfgWithConfigs" }
if (-not (Test-Path $templateCfgBase))        { throw "Template manquant : $templateCfgBase" }

Copy-Item -Path (Join-Path $templateCfgWithConfigs '*') -Destination $cfgWithConfigs -Recurse -Force
Copy-Item -Path (Join-Path $templateCfgBase '*')        -Destination $cfgBase        -Recurse -Force

$zipSpecs = @(
    @{ Name = "SPAWN-EDITOR-$Version.zip";            Source = (Join-Path $pluginRoot 'pkg-with-configs\*') },
    @{ Name = "SPAWN-EDITOR-$Version-no_configs.zip"; Source = (Join-Path $pluginRoot 'pkg-no-configs\*')  },
    @{ Name = "SPAWN-EDITOR-$Version-config.zip";     Source = (Join-Path $pluginRoot 'config-base\*')     }
)

foreach ($zip in $zipSpecs) {
    $zipPath = Join-Path $pluginRoot $zip.Name
    if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }
    Compress-Archive -Path $zip.Source -DestinationPath $zipPath -CompressionLevel Optimal -Force
}

Write-Host "Plugin artifacts synchronized to: $pluginRoot"
