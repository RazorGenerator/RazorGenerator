param($installPath, $toolsPath, $package, $project)

# set the test page's custom tool to the razor generator
$testPage = $project.ProjectItems.Item("SampleTemplate.cshtml")
$testPage.Properties.Item("CustomTool").Value = "RazorGenerator"
