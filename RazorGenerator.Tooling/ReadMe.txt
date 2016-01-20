Most of the code in the VSIX is standard boilerplate that comes from the Single File Generator Sample Deep Dive sample: http://code.msdn.microsoft.com/sfgdd.

The tool consists of 3 types of generators
* RazorHelperGenerator [ASP.NET Web Pages] - Generates types derived from System.Web.WebPages.HelperPage that are meant for packaging cshtml helper files into an assembly.
* RazorClassGenerator [ASP.NET Web Pages]- Generates types derived from System.Web.WebPages.WebPage that are best suited for Application Parts (_Admin modules)
* RazorMvcHelperGenerator [ASP.NET MVC] - Generates static types that can be used to create Mvc extension methods.

