$potentialMsBuildPaths = @(
    [IO.Path]::Combine(${env:ProgramFiles(x86)}, "MsBuild", "14.0", "Bin", "msbuild.exe"),
    [IO.Path]::Combine(${env:ProgramFiles(x86)}, "MsBuild", "12.0", "Bin", "msbuild.exe"),
    [IO.Path]::Combine($env:Windir, "Microsoft.NET", "Framework", "v4.0.30319", "msbuild.exe")
)
$msbuild = $potentialMsBuildPaths | ?{ Test-Path $_ } | Select -First 1

.nuget\nuget.exe restore .\MsBuildTest\src\packages.config -out packages
.nuget\nuget.exe install RazorGenerator.MsBuild -excludeversion -source $PWD\artifacts -out packages

&$msbuild ".\MsBuildTest\src\MsBuildTest.csproj"

ls -r .\MsBuildTest\src\obj\CodeGen *.cs | %{
    $sourceFile = $_
    $generatedContent = Get-Content $_.FullName | %{ 
        [Regex]::Replace($_, "(\d+\.){3}\d+", "x.x.x.x")
    }
    $expectedContent = Get-Content $_.FullName.Replace("src\obj\CodeGen", "result")
    $differences = Compare-Object -Casesensitive $expectedContent $generatedContent
    if ($differences) {
        $result = ""
        $differences | %{ 
            $result += $_.InputObject + "`r`n'"
        }
        
        throw "Differences in $sourceFile found: `r`n $result"
    }
}


