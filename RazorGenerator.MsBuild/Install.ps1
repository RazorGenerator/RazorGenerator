param($installPath, $toolsPath, $package, $project) 

function Ensure-GlobalImport($nugetDir) {
    $nugetTargetPath = Join-Path $nugetDir "NuGet.targets"
    
    $targetXml = [xml](Get-Content $nugetTargetPath)
    $importTarget = "..\packages\**\tools\NuGet.import.targets"
    if (($targetXml.Project.Import | Where{ $_.Project -eq $importTarget })) {
        return;
    }

    $importNode = $targetXml.CreateElement("Import", "http://schemas.microsoft.com/developer/msbuild/2003")
    $importNode.SetAttribute("Project", $importTarget)
    
    $targetXml.Project.AppendChild($importNode)

    $targetXml.Save($nugetTargetPath)
}

try {
    $solutionRoot = [IO.Path]::GetDirectoryName($project.DTE.Solution.FullName)
    $nugetDir = Join-Path $solutionRoot ".nuget"
    $packagesDir = Join-Path $solutionRoot "packages"
    if (!(Test-Path $nugetDir) -or !(Test-Path $packagesDir)) {
        return;
    }
    Ensure-GlobalImport $nugetDir $packagesDir
    
    $msBuildProject = @([Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName))[0]
    $msBuildProject.SetProperty("PrecompileRazorFiles", "true") | Out-Null
    $msBuildProject.Save()
} catch {
    Write-Warning $_
}