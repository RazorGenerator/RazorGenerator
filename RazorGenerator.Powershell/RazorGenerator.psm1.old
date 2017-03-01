function Resolve-ProjectName {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$ProjectName
    )
    
    if($ProjectName) {
        Get-Project $ProjectName
    }
    else {
        Get-Project
    }
}

function Get-ProjectFiles {
    Process {
        if ($_.Kind -eq "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}" -or $_.Kind -eq "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") {
            $_.ProjectItems | Get-ProjectFiles
        }
        else {
            $_
        }
    }
}

function Set-CustomTool {
    param($item, [string]$customTool, [bool]$force = $false)
    
    $customToolProperty = $_.Properties.Item("CustomTool")
    if ($force -or ($customTool -ne $customToolProperty.Value)) {
        $customToolProperty.Value = $customTool
        $_.Object.RunCustomTool()
        return $true
    }
}

function Get-RazorFiles {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$ProjectName
    )
    
    (Resolve-ProjectName $ProjectName).ProjectItems | Get-ProjectFiles | Where { $_.Name.EndsWith('.cshtml')  }
}

function Change-CustomTool {
    param (
        [string]$ProjectName,
        [string]$CustomTool
    )
    
    Process {
        $solutionExplorer = ($dte.Windows | Where { $_.Type -eq "vsWindowTypeSolutionExplorer" }).Object
        $project = (Resolve-ProjectName $projectName)
        $projectPath = [IO.Path]::GetDirectoryName($project.FullName)
        $ProjectName = $project.Name
		$SolutionName = [IO.Path]::GetFileNameWithoutExtension($dte.Solution.FullName)
        
        Get-RazorFiles $ProjectName | % { 
            if (Set-CustomTool $_ $CustomTool) {
                $relativePath = Get-RelativePath $projectPath $_
                if ($relativePath) {
                    $solutionExplorer.GetItem("$SolutionName\$ProjectName$relativePath").UIHierarchyItems.Expanded = $false
                }
            }
        }
    }
}

function Enable-RazorGenerator {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$ProjectName
    )
    
    Change-CustomTool $ProjectName "RazorGenerator"
    
}

function Disable-RazorGenerator {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$ProjectName
    )
    Change-CustomTool $ProjectName ""
}

function Redo-RazorGenerator {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$ProjectName
    )
    Process {
        Get-RazorFiles $ProjectName | % { 
            if ($_.Properties.Item("CustomTool")) {
                $_.Object.RunCustomTool()
            }
        }
    }
}

function Get-RelativePath {
    param($projectPath, $file) 
    $filePath = $file.Properties.Item("FullPath").Value
    
    $index = $filePath.IndexOf($projectPath)
    if ($index -ge 0) {
        $filePath.Substring($projectPath.Length)
    }
}

Export-ModuleMember Enable-RazorGenerator, Redo-RazorGenerator, Disable-RazorGenerator