# RazorGenerator

RazorGenerator.Mvc: [![NuGet Status](http://img.shields.io/nuget/v/RazorGenerator.Mvc.svg?style=flat-square)](https://www.nuget.org/packages/RazorGenerator.Mvc)

RazorGenerator.MsBuild: [![NuGet Status](http://img.shields.io/nuget/v/RazorGenerator.MsBuild.svg?style=flat-square)](https://www.nuget.org/packages/RazorGenerator.MsBuild)

RazorGenerator.Testing: [![NuGet Status](http://img.shields.io/nuget/v/RazorGenerator.Testing.svg?style=flat-square)](https://www.nuget.org/packages/RazorGenerator.Testing)

RazorGenerator.Templating: [![NuGet Status](http://img.shields.io/nuget/v/RazorGenerator.Templating.svg?style=flat-square)](https://www.nuget.org/packages/RazorGenerator.Templating)

TeamCity Build Status: [![Build status](http://razorgen-ci.cloudapp.net/app/rest/builds/buildType:\(id:RazorGenerator_RazorGenerator\)/statusIcon)](http://razorgen-ci.cloudapp.net/viewType.html?buildTypeId=btN&guest=1)


This is a Custom Tool for Visual Studio that allows processing Razor files at design time instead of runtime, allowing them to be built into an assembly for simpler reuse and distribution. 

Note that this tool currently only supports C#. VB support could probably be done if someone wants to help out with it!

## Installation instructions

It’s on the VS extension gallery, so install it from there. It’s called “Razor Generator” (not to be confused with “Razor Single File Generator for MVC”).

## Generator Types

- **`MvcHelper`**: Creates a static type that is best suited for writing Mvc specific helper methods.
- **`MvcView`**: Create a WebViewPage which allows the use of precompiled MVC views.
- **`WebPage`**: Creates a WebPage type that can be used as WebPages Application Part (such as _Admin and RazorDebugger).
- **`WebPagesHelper`**: Creates a HelperPage type that is suited for precompiling and distributing WebPages helper.
- **`Template`**: Generator based on T4 preprocessed template.

## Usage in an MVC app

- Install the 'RazorGenerator.Mvc' package, which registers a special view engine
- Go to an MVC Razor view's property and set the **`Custom Tool`** to **`RazorGenerator`**
- Optionally specify a value for `Custom Tool Namespace` to specify a namespace for the generated file. The project namespace is used by default.
- Optionally specify one of the generators in the first line of your Razor file. A generator declaration line looks like this: `@* Generator: MvcHelper *@`. If you don't specify this, a generator is picked based on convention (e.g. files under Views are treated as `MvcViews`)  NOTE: The selection has other criteria such as detecting "Helper" in the filename and choosing **`WebPagesHelper`** for those.
- You'll see a generated .cs file under the .cshtml file, which will be used at runtime instead of the .cshtml file
- You can also go to the nuget Package Manager Console and run `Enable-RazorGenerator` to enable the Custom Tool on all the views.
- And to cause all the views to be regenerated, go to the nuget Package Manager Console and run `Redo-RazorGenerator`. This is useful when you update the generator package and it needs to generate different code.

## Usage in a View Library

If you need to create a separate library for your precompiled MVC views, the best approach is to actually create an MVC project for that library, instead of a library project. You'll never actually run it as an Mvc app, but the fact that it comes with the right set of config files allows intellisense and other things to work a lot better than in a library project.

You can then add a reference to that 'MVC View project' from your real MVC app.

And note that you need to install the 'RazorGenerator.Mvc' package into the library, not the main MVC app.

See https://github.com/davidebbo/MvcApplicationRazorGeneratorSeparateLibrary for a sample.

## Special Razor directives

These directives go into a Razor comment at the top of a cshtml file. Multiple directives are simply whitespace-separated within the comment:

    @* Generator: Template  GeneratePrettyNames : true *@

You can also have them on separate lines for clarity but must remain part of the _same_ comment, e.g:

    @* Generator: Template  
       GeneratePrettyNames : true *@

#### Generator type

    @* Generator: MvcHelper *@

See above for list of valid values.

#### disabling line pragmas

    @* DisableLinePragmas: true *@

#### Using absolute paths in line pragmas instead of relative

    @* GenerateAbsolutePathLinePragmas: true *@

#### Overriding the generated namespace

    @* Namespace: SomeNamespace *@

#### Adding a suffix to the generated class name (can be useful to avoid some conflicts)

    @* ClassSuffix: _ *@

#### Generate namespaces and class names based on file name instead of the Razor default

    @* GeneratePrettyNames : true *@

#### Trimming leading underscores in generated class names

    @* TrimLeadingUnderscores : true *@

#### Adding generic type parameters

    @* Generator: Template GenericParameters: TKey, TValue *@

#### Add ExcludeForCodeCoverageAttribute to generated files
    @* ExcludeForCodeCoverage : true *@

#### Import additional namespaces during view precompilation

    @* Imports: Microsoft.Web.Mvc, MvcContrib, This.WebSite, This.WebSite.Html, HtmlHelpers.BeginCollectionItem *@

As an alternative, you can create a file named `razorgenerator.directives` in the Views folder to apply directives globally. e.g. it could contain:

    GeneratePrettyNames: true  GenerateAbsolutePathLinePragmas: true

## History

#### 8/26/2/2015 VSIX 1.9

- Add generic type parameters directive. https://github.com/RazorGenerator/RazorGenerator/issues/34

#### 3/19/2015 RazorGenerator.Templating 2.3.3

Add PCL support to RazorGenerator.Templating.

https://github.com/RazorGenerator/RazorGenerator/pull/1

#### 9/2/2014 VSIX 1.6.4

- Improve MVC version detection logic. https://razorgenerator.codeplex.com/discussions/459467

#### 1/9/2014 VSIX 1.6.3

- Fix EditorTemplate issue https://razorgenerator.codeplex.com/workitem/133

#### 12/02/2013 VSIX 1.6.2

- Fix MVC5 detection logic https://razorgenerator.codeplex.com/discussions/459467

#### 11/22/2013 VSIX 1.6.1

- Fix issue 132: crash when two directives conflict https://razorgenerator.codeplex.com/workitem/132

#### 11/6/2013 RazorGenerator.Mvc v2.2.1

- Fix more ~/ path logic https://razorgenerator.codeplex.com/SourceControl/network/forks/beelineuk/RazorGeneraorForSitecore/contribution/5619

#### 10/28/2013 VSIX 1.6

- Added MVC5/Razor3 support

#### 8/28/2013 VSIX 1.5.6

- Added VS 2013 support + added license file to VSIX

#### 8/22/2013 RazorGenerator.Mvc v2.1.2

- Fix issue #124: bad ~/ path logic https://razorgenerator.codeplex.com/workitem/124

#### 6/19/2013 RazorGenerator.Mvc v2.1.1

- Fix MapPath issue

#### 6/5/2013 Runtime v2.1.0

- New CompositePrecompiledMvcEngine https://razorgenerator.codeplex.com/SourceControl/network/forks/odinserj/RazorGenerator/contribution/4780

#### 5/4/2013 Runtime v2.0.1

- Fixed regression https://razorgenerator.codeplex.com/workitem/107

#### 4/25/2013

- v2.0: All the runtime assemblies are now signed.
- View activation process unification https://razorgenerator.codeplex.com/workitem/105

#### 10/14/2012

- Version 1.5 of the VSIX adds support for Razor 2.0 syntax. It auto detects whether it should use 1.0 or 2.0 based on the assembly being referenced by the project.

#### 4/20/2012

- Version 1.4 of RazorGenerator.Mvc issue with multiple registered view engines (http://razorgenerator.codeplex.com/workitem/55), adds support for overriding the layout page when returning ViewResults and fixes a bug in UsePhysicalViewsIfNewer

#### 1/8/2012

- Version 1.2.3 of the VSIX fixes MVC4 issue (http://razorgenerator.codeplex.com/workitem/26)
- Version 1.3.1 of RazorGenerator.Testing fixes an issue with RenderAsHtml not flowing the context

#### 1.2 (8/29/2011)

- Renamed all packages to be RazorGenerator.*
- Fix issue http://razorgenerator.codeplex.com/workitem/22 (Line pragmas should not use absolute paths)

#### 1.1.2 (8/4/2011)

- Fixed encoding issue when using ANSI (non UTF-8) razor files (http://razorgenerator.codeplex.com/workitem/15)
- Support null values in simple template scenario. e.g. @foo when foo is null now renders empty string. (http://razorgenerator.codeplex.com/workitem/16)

#### 1.1.1 (7/11/2011)

- Change the generated file extensions from foo.cs to foo.generated.cs (http://razorgenerator.codeplex.com/workitem/8)

#### 1.1 (7/7/2011)

- The generator now finds imported namespaces defined in web.config
- Add support for project level directives in a razorgenerator.directives file

#### 1.0.1 (6/21/2011)

- Fix generated line pragmas

#### 1.0

- Original release

## Related blog posts:

- http://blog.davidebbo.com/tag/#RazorGenerator
- http://blogs.msdn.com/b/davidebb/archive/2010/10/27/turn-your-razor-helpers-into-reusable-libraries.aspx
