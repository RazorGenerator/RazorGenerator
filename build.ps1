$potentialMsBuildPaths = @(
    [IO.Path]::Combine(${env:ProgramFiles(x86)}, "MsBuild", "12.0", "Bin"),
    [IO.Path]::Combine(${env:ProgramFiles(x86)}, "MsBuild", "14.0", "Bin"),
    [IO.Path]::Combine($env:Windir, "Microsoft.NET\Framework\v4.0.30319\msbuild.exe")
)
$msbuild = Join-Path ($potentialMsBuildPaths | ? { Test-Path $_ } | Select -First 1) "msbuild.exe"

& $msbuild .nuget\nuget.targets /t:_DownloadNuGet /v:M
.nuget\NuGet.exe restore RazorGenerator.Tooling.sln
.nuget\NuGet.exe restore RazorGenerator.Runtime.sln

& $msbuild /p:Configuration=Release RazorGenerator.Tooling.sln /v:M
& $msbuild /p:Configuration=Release RazorGenerator.Runtime.sln /v:M
