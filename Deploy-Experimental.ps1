[CmdletBinding()]
param(
    [switch] $Launch,
    [switch] $SkipBuild,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Installer and vswhere.exe are required.'
}

$instances = @(
    & $vswhere -latest -prerelease -products '*' -version '[18.0,19.0)' -requires Microsoft.Component.MSBuild -format json |
        ConvertFrom-Json
)
if ($LASTEXITCODE -ne 0 -or $instances.Count -eq 0) {
    throw 'No VS 2026 installation with MSBuild was found.'
}
$instance = $instances[0]
$devenv = Join-Path $instance.installationPath 'Common7\IDE\devenv.exe'

$runningExp = Get-CimInstance Win32_Process -Filter "Name='devenv.exe'" |
    Where-Object {
        $_.ExecutablePath -eq $devenv -and
        $_.CommandLine -match '(?i)/rootsuffix\s+"?Exp(?:"|\s|$)'
    }
if ($runningExp) {
    throw 'Close the experimental Visual Studio instance before deploying. Normal VS instances can remain open.'
}

$project = Join-Path $PSScriptRoot 'src\ThemeFontWeight\ThemeFontWeight.fsproj'
if (-not $SkipBuild) {
    Push-Location $PSScriptRoot
    try {
        & dotnet build $project -c $Configuration --nologo -v minimal
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

$vsix = Join-Path $PSScriptRoot "src\ThemeFontWeight\bin\$Configuration\net48\ThemeFontWeight.vsix"
if (-not (Test-Path $vsix)) {
    throw "The VSIX has not been built: $vsix"
}

# Use the installer engine directly so we wait for installation, not its launcher.
$installer = Join-Path $instance.enginePath 'VsixInstaller\VSIXInstaller.exe'
if (-not (Test-Path $installer)) {
    throw "VSIX installer engine not found: $installer"
}
$install = Start-Process $installer -ArgumentList @(
    '/quiet', '/rootSuffix:Exp', "/instanceIds:$($instance.instanceId)", "`"$vsix`""
) -PassThru -Wait
if ($install.ExitCode -ne 0) {
    throw "Experimental installation failed with exit code $($install.ExitCode). See the VSIX installer log in TEMP."
}

# Exp must merge new package/settings manifests before the settings UI is opened.
$update = Start-Process $devenv -ArgumentList '/RootSuffix Exp /UpdateConfiguration' -PassThru -Wait
if ($update.ExitCode -ne 0) {
    throw "Experimental configuration refresh failed with exit code $($update.ExitCode)."
}

if ($Launch) {
    Start-Process $devenv -ArgumentList '/RootSuffix Exp /Log'
}
