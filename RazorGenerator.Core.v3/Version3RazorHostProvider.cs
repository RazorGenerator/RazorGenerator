using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    [Export(typeof(IRazorHostProvider))]
    public class Version3RazorHostProvider : IRazorHostProvider
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
                Type webPagesDeploymentType      = typeof(System.Web.WebPages.Deployment.WebPagesDeployment);
                Type objectInfoType              = typeof(System.Web.Helpers.ObjectInfo); // not used by this project, but used by this method to get an assembly reference to System.Web.Helpers.dll

                // Reminder: https://stackoverflow.com/questions/4396290/what-does-this-square-bracket-and-parenthesis-bracket-notation-mean-first1-last
                // `[MinInclusive,MaxExclusive)`

                // System.Web                      should be `[4.0.0,5.0.0)`
                // System.Web.Helpers              should be `[3.0.0,4.0.0)`
                // System.Web.Mvc                  should be `[5.0.0,6.0.0)`
                // System.Web.Razor                should be `[3.0.0,4.0.0)`
                // System.Web.WebPages             should be `[3.0.0,4.0.0)`
                // System.Web.WebPages.Deployment  should be `[3.0.0,4.0.0)`
                // System.Web.WebPages.Razor       should be `[3.0.0,4.0.0)`
                    
                Assembly systemWebDll                   = webConfigSection.Assembly;
                Assembly systemWebHelpersDll            = objectInfoType.Assembly;
                Assembly systemWebMvcDll                = viewStartPageType.Assembly;
                Assembly systemWebRazorDll              = generatedClassContextType.Assembly;
                Assembly systemWebWebPagesDll           = helperPageType.Assembly;
                Assembly systemWebWebPagesDeploymentDll = helperPageType.Assembly;
                Assembly systemWebWebPagesRazorDll      = razorPagesConfigSectionType.Assembly;

                Boolean ok = 
                    systemWebDll                  .VersionIsInRange( new Version( 4, 0, 0 ), new Version( 5, 0, 0 ), out String err1 ) &
                    systemWebHelpersDll           .VersionIsInRange( new Version( 3, 0, 0 ), new Version( 4, 0, 0 ), out String err2 ) &
                    systemWebMvcDll               .VersionIsInRange( new Version( 5, 0, 0 ), new Version( 6, 0, 0 ), out String err3 ) &
                    systemWebRazorDll             .VersionIsInRange( new Version( 3, 0, 0 ), new Version( 4, 0, 0 ), out String err4 ) &
                    systemWebWebPagesDll          .VersionIsInRange( new Version( 3, 0, 0 ), new Version( 4, 0, 0 ), out String err5 ) &
                    systemWebWebPagesDeploymentDll.VersionIsInRange( new Version( 3, 0, 0 ), new Version( 4, 0, 0 ), out String err6 ) &
                    systemWebWebPagesRazorDll     .VersionIsInRange( new Version( 3, 0, 0 ), new Version( 4, 0, 0 ), out String err7 );

                 errorDetails = String.Join( separator: Environment.NewLine, new String[] { err1, err2, err3, err4, err5, err6, err7 }.Where( s => !String.IsNullOrWhiteSpace( s ) ) );

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
            return new Version3RazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
        }
    }
}
