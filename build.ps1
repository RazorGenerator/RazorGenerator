# Locate MSBuild using vswhere (supports VS2022 / VS2026 and later)
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    $vswhere = "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
}

if (Test-Path $vswhere) {
    $vsInstallPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    $msbuild = Join-Path $vsInstallPath "MSBuild\Current\Bin\MSBuild.exe"
} else {
    # Fallback: try well-known VS2022 / VS2026 paths
    $potentialMsBuildPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2026\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2026\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2026\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    $msbuild = $potentialMsBuildPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $msbuild -or -not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found. Please install Visual Studio 2022 or later."
    exit 1
}

Write-Host "Using MSBuild: $msbuild"

# Restore NuGet packages
.nuget\NuGet.exe restore RazorGenerator.Tooling.sln
.nuget\NuGet.exe restore RazorGenerator.Runtime.sln

& $msbuild /p:Configuration=Release RazorGenerator.Tooling.sln /v:M
& $msbuild /p:Configuration=Release RazorGenerator.Runtime.sln /v:M
& $msbuild /p:Configuration=Release RazorGenerator.Core.Test/RazorGenerator.Core.Test.csproj /t:Test