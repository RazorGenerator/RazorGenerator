Function Resolve-ProjectName {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)][string]$projectName
    )

    If( $projectName ) {
        Get-Project $projectName
    }
    Else {
        Get-Project
    }
}

Function Get-ProjectFiles {
    Begin {
        $physicalFolderGuid = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}" # https://msdn.microsoft.com/en-us/library/bb166496.aspx
        $solutionFolderGuid = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}" # https://msdn.microsoft.com/en-us/library/hb23x61k(v=vs.80).aspx
    }
    Process {
        If( $_.Kind -eq $physicalFolderGuid -or $_.Kind -eq $solutionFolderGuid ) {
            $_.ProjectItems | Get-ProjectFiles
        }
        Else {
            $_
        }
    }
}

Function Set-CustomTool {
    Param(
        $item,
        [string]$customTool,
        [bool]$force = $false
    )
    
    $customToolProperty = $_.Properties.Item("CustomTool")
    If( $force -or ( $customTool -ne $customToolProperty.Value ) ) {
        $customToolProperty.Value = $customTool
        
        Write-Debug "RunCustomTool - $( $_.Name ) - $( $customTool )"
        $_.Object.RunCustomTool()

        return $true
    }
}

Function Get-RazorFiles {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$projectName
    )
    
    (Resolve-ProjectName $projectName).ProjectItems | Get-ProjectFiles | Where { $_.Name.EndsWith('.cshtml')  }
}

Function Get-SolutionExplorerNodes {
    
    $map = New-Object 'System.Collections.Generic.Dictionary[System.String,EnvDTE.UIHierarchyItem]'

    # `.Object` returns an object that implements `interface UIHierarchy`: https://msdn.microsoft.com/en-us/library/envdte.uihierarchy.aspx
    $solutionExplorer = ($dte.Windows | Where { $_.Type -eq "vsWindowTypeSolutionExplorer" }).Object 

    If( $solutionExplorer -ne $null -and $solutionExplorer -is [EnvDTE.UIHierarchy] ) {
        
        Fill-SolutionExplorerNodes 1 $map $solutionExplorer.UIHierarchyItems
    }
    Else {
        
        Write-Warning "Solution Explorer window not found. No nodes will be collapsed."
    }

    Return $map
}

# Populates a dictionary that maps filenames to *visible* Solution Explorer nodes. The `.Object` property of each UIHierarchyItem is often, but not necessarily always, an EnvDTE.ProjectItem instance.
# NOTE: The EnvDTE assemblies are loaded as-per the *.psd1 file's RequiredAssemblies directive.
Function Fill-SolutionExplorerNodes {
    Param(
        [int]$depth,
        [System.Collections.Generic.Dictionary[System.String,EnvDTE.UIHierarchyItem]]$map,
        [EnvDTE.UIHierarchyItems]$nodes
    )

    ForEach( $uiHierarchyItem in $nodes ) {

        $projectItem = $uiHierarchyItem.Object # $projectItem.GetType() always returns System.__ComObject
        If( $projectItem -ne $null ) {

            Write-Debug "$( $projectItem.GetType().FullName )"

            If( !( $projectItem -is [EnvDTE.ProjectItem] ) ) {
                Write-Debug "Skipping - `$projectItem is not EnvDTE.ProjectItem"
            }
            ElseIf( $projectItem.FileCount -ne 1 ) {
                Write-Debug "Skipping - FileCount.Length == $( $projectItem.FileCount )"
            }
            Else {
                
                $fileName = $projectItem.FileNames(1) # `FileNames` is a COM Indexed Property with a base of 1, there is no `FileNames.Length` property, see `FileCount` instead.
                Write-Debug $fileName

                If( !$map.ContainsKey( $fileName ) ) { # guard, because Solution Explorer can have multiple nodes for the same file, for simplicity this code only uses the first appearance.
                
                    $map.Add( $fileName, $uiHierarchyItem )
                }
            }
        }
        Else {
            Write-Debug "`$uiHierarchyItem ($( $uiHierarchyItem.Name )).Object == `$null"
        }

        # Note that if a node is collapsed (e.g. a collapsed project or folder) then `$uiHierarchyItem.UIHierarchyItems.Count == 0` and this function won't recurse.
        # So this function will not populate the map with *all* ProjectItems, only those that are visible.
        Write-Debug "Node `"$( $uiHierarchyItem.Name )`" child count: $( $uiHierarchyItem.UIHierarchyItems.Count )"
        If( ( $uiHierarchyItem.UIHierarchyItems -is [EnvDTE.UIHierarchyItems] ) -and ( $uiHierarchyItem.UIHierarchyItems.Count -gt 0 ) ) {
            
            Fill-SolutionExplorerNodes ($depth + 1) $map $uiHierarchyItem.UIHierarchyItems
        }
    }

}

Function Change-CustomTool {
    Param(
        [string]$projectName,
        [string]$CustomTool
    )

    # StrictMode enables errors about nonexistent properties which is useful when working with COM Automation objects.
    Set-StrictMode -Version 2.0 

    $projectName = (Resolve-ProjectName $projectName).Name

    $solutionExplorerNodeMap = Get-SolutionExplorerNodes

    Get-RazorFiles $projectName | % { # `%` means ForEach-Object
            
        # If the RazorGenerator CustomTool is applied to a file not previously marked, then collapse the
        # node afterwards because VS will show the new children by default, so any *.generated.cs files
        # that are already visible will not be hidden.
        If( Set-CustomTool $_ $CustomTool ) {
            $fileName = $_.Properties.Item("FullPath").Value
                
            $uiHierarchyItem = $null
            If( $solutionExplorerNodeMap.TryGetValue( $fileName, [ref] $uiHierarchyItem ) ) {
                    
                $uiHierarchyItem.UIHierarchyItems.Expanded = $false
            }
        }
    }

    Set-StrictMode -Off
}

Function Enable-RazorGenerator {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)][string]$projectName
    )

    Change-CustomTool $projectName "RazorGenerator"
}

Function Disable-RazorGenerator {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)][string]$projectName
    )

    Change-CustomTool $projectName ""
}

Function Redo-RazorGenerator {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)][string]$projectName
    )

    Get-RazorFiles $projectName | % { 
        If( $_.Properties.Item( "CustomTool" ) ) {
            $_.Object.RunCustomTool()
        }
    }
}

# Expands every `.cshtml` file so its `*.generated.cs` files are visible in Solution Explorer.
Function Show-RazorGeneratedFiles {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)][string]$projectName
    )

    Set-StrictMode -Version 2.0

    $projectName = (Resolve-ProjectName $projectName).Name
        
    $solutionExplorerNodeMap = Get-SolutionExplorerNodes

    Get-RazorFiles $projectName | % { # `%` means ForEach-Object
    
        $fileName = $_.Properties.Item("FullPath").Value    

        $uiHierarchyItem = $null
        If( $solutionExplorerNodeMap.TryGetValue( $fileName, [ref] $uiHierarchyItem ) ) {
                    
            $uiHierarchyItem.UIHierarchyItems.Expanded = $true
        }
    }

    Set-StrictMode -Off
}

# Collapses every `.cshtml` file so its `*.generated.cs` files are no-longer visible in Solution Explorer.
Function Hide-RazorGeneratedFiles {
    Param(
        [parameter(ValueFromPipelineByPropertyName = $true)][string]$projectName
    )

    Set-StrictMode -Version 2.0

    $projectName = (Resolve-ProjectName $projectName).Name
        
    $solutionExplorerNodeMap = Get-SolutionExplorerNodes

    Get-RazorFiles $projectName | % { # `%` means ForEach-Object
    
        $fileName = $_.Properties.Item("FullPath").Value    

        $uiHierarchyItem = $null
        If( $solutionExplorerNodeMap.TryGetValue( $fileName, [ref] $uiHierarchyItem ) ) {
                    
            $uiHierarchyItem.UIHierarchyItems.Expanded = $false
        }
    }

    Set-StrictMode -Off
}

Export-ModuleMember Enable-RazorGenerator, Redo-RazorGenerator, Disable-RazorGenerator, Show-RazorGeneratedFiles, Hide-RazorGeneratedFiles