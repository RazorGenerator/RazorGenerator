function Resolve-ProjectName {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string[]]$ProjectName
    )
    
    if($ProjectName) {
        $projects = Get-Project $ProjectName
    }
    else {
        # All projects by default
        $projects = Get-Project -All
    }
    
    $projects
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
    
    $customToolProperty = $_.Properties | Where { $_.Name -eq "CustomTool" }
    if ($force -or !$customToolProperty.Value) {
        $customToolProperty.Value = $customTool
    }
}

function Get-RazorFiles {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string[]]$ProjectName
    )
    
    (Resolve-ProjectName $ProjectName) | % {
        Get-ProjectFiles | Where { $_.Name.EndsWith('.cshtml') }
    }

function Initialize-RazorGenerator {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string[]]$ProjectName
    )
    Process {
        Get-RazorFiles $ProjectName | % { Set-CustomTool $_ "RazorGenerator" }
    }
}

function Redo-RazorGenerator {
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string[]]$ProjectName
    )
    Process {
        Get-RazorFiles $ProjectName | % { 
            Set-CustomTool $_ "" $true;
            Set-CustomTool $_ "RazorGenerator" $true;
        }
    }
}

Export-ModuleMember Initialize-RazorGenerator, Redo-RazorGenerator