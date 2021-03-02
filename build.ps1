#$potentialMsBuildPaths = @(
#    [IO.Path]::Combine(${env:ProgramFiles(x86)}, "MsBuild", "12.0", "Bin"),
#    [IO.Path]::Combine(${env:ProgramFiles(x86)}, "MsBuild", "14.0", "Bin"),
#    [IO.Path]::Combine($env:Windir, "Microsoft.NET", "Framework", "v4.0.30319")
#)
#$msbuild = $potentialMsBuildPaths | Join-Path -ChildPath "msbuild.exe" | ? { Test-Path $_ } | Select -First 1

$vswhere = [IO.Path]::Combine(${env:ProgramFiles(x86)}, "Microsoft Visual Studio", "Installer", "vswhere.exe");

$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1

& $msbuild .nuget\nuget.targets /t:_DownloadNuGet /v:M
.nuget\NuGet.exe restore RazorGenerator.Tooling.sln
.nuget\NuGet.exe restore RazorGenerator.Runtime.sln

& $msbuild /p:Configuration=Release RazorGenerator.Tooling.sln /v:M
& $msbuild /p:Configuration=Release RazorGenerator.Runtime.sln /v:M
& $msbuild /p:Configuration=Release RazorGenerator.Core.Test/RazorGenerator.Core.Test.csproj /t:Test
