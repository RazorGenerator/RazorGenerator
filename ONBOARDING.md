
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

### The `MvcViewsTests` project won't load.

* The `MvcViewsTests` project has a Project Type GUID of {3AC096D0-A1C2-E12C-1390-A8335801FDAB}.
  * This is the Project Type GUID of old MSTest projects, this test project will need to be updated. TODO.
  * This is a bug in Visual Studio 2019: https://developercommunity.visualstudio.com/content/problem/833631/there-is-a-missing-project-subtype-3ac096d0-a1c2-e.html
	* Advice is to perform a Visual Studio 2019 installation repair.
    * I performed a Repair install just now (without adding/removing any VS components or features) and that fixed the problem.
