param($installPath, $toolsPath, $package, $project)

$rootProjectItems = $project.ProjectItems
$viewsProjectItems = $rootProjectItems.Item("Views").ProjectItems

# set the test view's custom tool to the razor generator
$testView = $viewsProjectItems.Item("Home").ProjectItems.Item("Test.cshtml")
$testView.Properties.Item("CustomTool").Value = "RazorGenerator"

# Copy the web.config files over to make intellisense work. Due to a nuget issue that special cases web.config files,
# they don't automatically get copied :(
$rootProjectItems.AddFromFileCopy((Join-Path $toolsPath "..\Content\Web.config"))
$viewsProjectItems.AddFromFileCopy((Join-Path $toolsPath "..\Content\Views\Web.config"))
