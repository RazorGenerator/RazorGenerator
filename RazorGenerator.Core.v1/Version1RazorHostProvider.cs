using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    [Export(typeof(IRazorHostProvider))]
    public class Version1RazorHostProvider : IRazorHostProvider
    {
        public Boolean CanGetRazorHost( out String errorDetails )
        {
            // Test all the required types are available.
            // ...and of the right version range.
            try
            {
                Type generatedClassContextType   = typeof(System.Web.Razor.Generator.GeneratedClassContext);
                Type webViewPageType             = typeof(System.Web.Mvc.WebViewPage);
                Type viewStartPageType           = typeof(System.Web.Mvc.ViewStartPage);
                Type mvcParser                   = typeof(System.Web.Mvc.Razor.MvcCSharpRazorCodeParser);
                Type webConfigSection            = typeof(System.Web.Configuration.WebConfigurationManager);
                Type razorPagesConfigSectionType = typeof(System.Web.WebPages.Razor.Configuration.RazorPagesSection);
                Type helperPageType              = typeof(System.Web.WebPages.HelperPage);

                // Reminder: https://stackoverflow.com/questions/4396290/what-does-this-square-bracket-and-parenthesis-bracket-notation-mean-first1-last
                // `[MinInclusive,MaxExclusive)`

                // System.Web                should be `[4.0.0,5.0.0)`
                // System.Web.Mvc            should be `[3.0.0,4.0.0)`
                // System.Web.Razor          should be `[1.0.0,2.0.0)`
                // System.Web.WebPages       should be `[1.0.0,2.0.0)`
                // System.Web.WebPages.Razor should be `[1.0.0,2.0.0)`
                    
                Assembly systemWebDll              = webConfigSection.Assembly;
                Assembly systemWebMvcDll           = viewStartPageType.Assembly;
                Assembly systemWebRazorDll         = generatedClassContextType.Assembly;
                Assembly systemWebWebPagesDll      = helperPageType.Assembly;
                Assembly systemWebWebPagesRazorDll = razorPagesConfigSectionType.Assembly;

                Boolean ok = 
                    systemWebDll             .VersionIsInRange( new Version( 4, 0, 0 ), new Version( 5, 0, 0 ), out String err1 ) &
                    systemWebMvcDll          .VersionIsInRange( new Version( 3, 0, 0 ), new Version( 4, 0, 0 ), out String err2 ) &
                    systemWebRazorDll        .VersionIsInRange( new Version( 1, 0, 0 ), new Version( 2, 0, 0 ), out String err3 ) &
                    systemWebWebPagesDll     .VersionIsInRange( new Version( 1, 0, 0 ), new Version( 2, 0, 0 ), out String err4 ) &
                    systemWebWebPagesRazorDll.VersionIsInRange( new Version( 1, 0, 0 ), new Version( 2, 0, 0 ), out String err5 );

                errorDetails = String.Join( separator: Environment.NewLine, new String[] { err1, err2, err3, err4, err5 }.Where( s => !String.IsNullOrWhiteSpace( s ) ) );

                return ok;
            }
            catch( TypeLoadException ex )
            {
                errorDetails = ex.ToString();
                return false;
            }
        }

        public IRazorHost GetRazorHost(
            string                      projectRelativePath,
            FileInfo                    fullPath,
            IOutputRazorCodeTransformer codeTransformer,
            CodeDomProvider             codeDomProvider,
            IDictionary<string,string>  directives
        )
        {
            return new Version1RazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
        }
    }
}
