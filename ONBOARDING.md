
# Onboarding steps

* When opening either solution for the first time, ensure you perform a NuGet Package Restore.

# Onboarding Issues

### Visual Studio 2019 reports missing assembly reference errors in IntelliSense output despite the project building successfully (i.e. no build errors):

1. Ensure you have the .NET Framework 4.0 (not just 4.5, 4.7.2, etc) SDKs/Targeting Packs installed.
1. Ensure the NuGet restore completed.
1. Perform a full rebuild of the project (Build > Rebuild Solution) without cleaning.
1. Restart VS2019 and reopen the solution.
1. The IntelliSense errors should be gone.
1. If they persist, see this report:
  * https://developercommunity.visualstudio.com/content/problem/483450/vs-2019-intellisense-reports-compile-errors-when-r.html
  * Tools > Options > Projects and Solutions > General > Allow parallel project initialization (uncheck, and restart VS)
  * Also consider nuking the `.vs` directory in the solution root. (And there should be no other `*.suo` or `*.csproj.user` files anywhere else in the solution directory structure).

### RazorGenerator.Tooling.csproj build issues:

#### Issue: `ShouldCleanDeployedVsixExtensionFiles`

When building `RazorGenerator.Tooling` from within VS2019, this build error is encountered:

> Invoke build failed due to exception 'Specified condition "$(ShouldCleanDeployedVsixExtensionFiles)" evaluates to "" instead of a boolean

This error disappeared after restarting Visual Studio 2019.

### The `MvcViewsTests` project won't load.

* The `MvcViewsTests` project has a Project Type GUID of {3AC096D0-A1C2-E12C-1390-A8335801FDAB}.
  * This is the Project Type GUID of old MSTest projects, this test project will need to be updated. TODO.
  * This is a bug in Visual Studio 2019: https://developercommunity.visualstudio.com/content/problem/833631/there-is-a-missing-project-subtype-3ac096d0-a1c2-e.html
	* Advice is to perform a Visual Studio 2019 installation repair.
    * I performed a Repair install just now (without adding/removing any VS components or features) and that fixed the problem.

## `\.build.ps1` issues

### Build errors from MSBuild (but not Visual Studio) regarding Microsoft.VisualStudio.QualityTools.UnitTestFramework

* `Microsoft.VisualStudio.QualityTools.UnitTestFramework` contains the MSTest (Visual Studio Unit Test) types like [TestClass] and [Test].
* The error happens in MSBuild but not Visual Studio because the UnitTestFramework assembly is local to Visual Studio but not systemwide.
* The general solution is to replace the UnitTestFramework reference with the `MSTest.Framework` NuGet package.
  * However, the NuGet package has a dependency on .NET Framework 4.5 - so if your project is .NET Framework 4.0 that's a blocking problem with no easy solution.
    * (or just upgrade to .NET Framework 4.5)

### Missing Visual Studio components?

* I modified my local VS2017 and VS2019 installations to both include the "Visual Studio extension development" workload.
* I also added all of the .NET Framework 3.5 through 4.8 SDKs and targeting packs (what's the difference again?)
* I saw a mix of few other .NET 4.x and Testing components were unchecked in either VS2017 and VS2019 - so I ensured they're all checked now.

###  GetDeploymentPathFromVsixManifest

Getting this error in `.\build.ps1` after MSBuild builds `RazorGenerator.Tooling`:

```
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018: The "GetDeploymentPathFromVsixManifest" task failed unexpectedly.\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018: System.ArgumentNullException: Value cannot be null.\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018: Parameter name: path1\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018:    at System.IO.Path.Combine(String path1, String path2)\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018:    at Microsoft.VisualStudio.Sdk.BuildTasks.ExtensionManagerUtilities.GetSettingsManagerForDevenv(String rootSuffix)\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018:    at Microsoft.VisualStudio.Sdk.BuildTasks.GetDeploymentPathFromVsixManifest.Execute()\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018:    at Microsoft.Build.BackEnd.TaskExecutionHost.Microsoft.Build.BackEnd.ITaskExecutionHost.Execute()\r [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
C:\git\me\RazorGenerator\packages\Microsoft.VisualStudio.Sdk.BuildTasks.14.0.14.0.12-pre\tools\VSSDK\Microsoft.VsSDK.targets(644,5): error MSB4018:    at Microsoft.Build.BackEnd.TaskBuilder.<ExecuteInstantiatedTask>d__26.MoveNext() [C:\git\me\RazorGenerator\RazorGenerator.Tooling\RazorGenerator.Tooling.csproj]
```

Mentioned here (note the same top-level message is used for different reasons, note the second line for the actual exception) https://github.com/Microsoft/VSSDK-Extensibility-Samples/issues/116
	Replies suggest it's a per-system configuration resulting from a b0rked VS install - hopefully installing the VS SDKs will fix this for me?

