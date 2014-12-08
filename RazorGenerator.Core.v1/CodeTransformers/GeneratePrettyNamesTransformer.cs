using System.Collections.Generic;
using System.IO;
using System.Web.Razor.Parser;

namespace RazorGenerator.Core
{
    public class GeneratePrettyNamesTransformer : RazorCodeTransformerBase
    {
        public static readonly string DirectiveName = "GeneratePrettyNames";
        public static readonly string VsNamespaceKey = "VsNamespace";
        private readonly bool _trimLeadingUnderscores;

        public GeneratePrettyNamesTransformer(bool trimLeadingUnderscores)
        {
            _trimLeadingUnderscores = trimLeadingUnderscores;
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            var fileName = Path.GetFileNameWithoutExtension(razorHost.ProjectRelativePath);
            var className = ParserHelpers.SanitizeClassName(fileName);
            if (_trimLeadingUnderscores)
            {
                className = className.TrimStart('_');
            }
            razorHost.DefaultClassName = className;

            // When generating pretty type names, if we also have ItemNamespace from VS, use it.
            string vsNamespace;
            if (directives.TryGetValue(VsNamespaceKey, out vsNamespace) && 
               !string.IsNullOrEmpty(vsNamespace))
            {
                razorHost.DefaultNamespace = vsNamespace;
            }
        }
    }
}
