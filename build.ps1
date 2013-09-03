$msbuild = Join-Path $env:Windir "Microsoft.NET\Framework\v4.0.30319\msbuild.exe"

& $msbuild .nuget\nuget.targets /t:_DownloadNuGet /v:M
.nuget\NuGet.exe restore RazorGenerator.Tooling.sln
.nuget\NuGet.exe restore RazorGenerator.Runtime.sln

& $msbuild RazorGenerator.Tooling.sln /v:M
& $msbuild RazorGenerator.Runtime.sln /v:M
