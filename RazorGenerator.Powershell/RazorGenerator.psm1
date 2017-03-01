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
		$physicalFolderGuid = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}" # https://msdn.microsoft.com/en-us/library/bb166496.aspx
		$solutionFolderGuid = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}" # https://msdn.microsoft.com/en-us/library/hb23x61k(v=vs.80).aspx

        if ($_.Kind -eq $physicalFolderGuid -or $_.Kind -eq $solutionFolderGuid) {
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
        [string]$projectName
    )
    
    (Resolve-ProjectName $projectName).ProjectItems | Get-ProjectFiles | Where { $_.Name.EndsWith('.cshtml')  }
}

# Populates a dictionary that maps filenames to Solution Explorer nodes.
# TODO: Make this function "private", because there's no need for it to be exposed to the user as a PowerShell command.
function Fill-SolutionExplorerNodes {
    param(
        [System.Collections.Generic.Dictionary[System.String,EnvDTE.UIHierarchyItem]]$map,
        [EnvDTE.UIHierarchyItems]$nodes
    )

    ForEach( $uiHierarchyItem in $nodes ) {
        
        If( !( $uiHierarchyItem -is [EnvDTE.UIHierarchyItem] ) ) {
            Continue
        }

        $projectItem = $uiHierarchyItem.Object
        If( ( $projectItem -is [EnvDTE.ProjectItem] ) -and ( $projectItem.FileNames.Length -eq 1 ) ) {
            
            $fileName = $projectItem.FileNames[0]
            
            If( !$map.ContainsKey( $fileName ) ) { # guard, because Solution Explorer can have multiple nodes for the same file, for simplicity this code only uses the first appearance.
                
                $map.Add( $fileName, $uiHierarchyItem )
            }
        }

        If( $uiHierarchyItem -is [EnvDTE.UIHierarchyItems] ) { # only recurse children if the node has children
            
            Fill-SolutionExplorerNodes $map $uiHierarchyItem
        }
    }

}

function Change-CustomTool {
    param (
        [string]$ProjectName,
        [string]$CustomTool
    )
    
    Process {
        $solutionExplorer = ($dte.Windows | Where { $_.Type -eq "vsWindowTypeSolutionExplorer" }).Object # returns an object that implements `interface UIHierarchy`: https://msdn.microsoft.com/en-us/library/envdte.uihierarchy.aspx
        $project = (Resolve-ProjectName $projectName)
        $projectPath = [IO.Path]::GetDirectoryName($project.FullName)
        $ProjectName = $project.Name
		$SolutionName = [IO.Path]::GetFileNameWithoutExtension($dte.Solution.FullName)
        
        $map = New-Object 'System.Collections.Generic.Dictionary[System.String,EnvDTE.UIHierarchyItem]'
        Fill-SolutionExplorerNodes $map $solutionExplorer.UIHierarchyItems

        Get-RazorFiles $ProjectName | % { 
            if (Set-CustomTool $_ $CustomTool) {
                $fileName = $_.Properties.Item("FullPath").Value
                
                $uiHierarchyItem = $null
                If( $map.TryGetValue( $fileName, [ref] $uiHierarchyItem ) ) {
                    
                    $uiHierarchyItem.UIHierarchyItems.Expanded = $false
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