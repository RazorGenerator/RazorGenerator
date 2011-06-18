using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace RazorGenerator.Core {
    [Export("WebPage", typeof(IRazorCodeTransformer))]
    public class WebPageTransformer : AggregateCodeTransformer {
        private static readonly IList<IRazorCodeTransformer> _transformers = new List<IRazorCodeTransformer> { 
            new DirectivesBasedTransformers(), 
            new AddGeneratedClassAttribute(),
            new AddPageVirtualPathAttribute(),
            new SetImports(new[] { "System.Web.WebPages.Html" }, replaceExisting: false),
        };

        protected override IEnumerable<IRazorCodeTransformer> CodeTransformers {
            get {
                return _transformers;
            }
        }

        public override void Initialize(RazorHost razorHost, string projectRelativePath, IDictionary<string, string> directives) {
            base.Initialize(razorHost, projectRelativePath, directives);

            // Remove the extension and replace path separator slashes with underscores
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(projectRelativePath);
            var pathWithoutExtenstion = Path.Combine(Path.GetDirectoryName(projectRelativePath), fileNameWithoutExtension);
            string className = pathWithoutExtenstion.TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '_');

            // If its a PageStart page, set the base type to StartPage.
            if (fileNameWithoutExtension.Equals("_pagestart", StringComparison.OrdinalIgnoreCase)) {
                razorHost.DefaultBaseClass = typeof(System.Web.WebPages.StartPage).FullName;
            }
        }


        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit,
                                                  CodeNamespace generatedNamespace,
                                                  CodeTypeDeclaration generatedClass,
                                                  CodeMemberMethod executeMethod) {
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);


            // Create the Href wrapper
            CodeTypeMember hrefMethod = new CodeSnippetTypeMember(@"
                // Resolve package relative syntax
                // Also, if it comes from a static embedded resource, change the path accordingly
                public override string Href(string virtualPath, params object[] pathParts) {
                    virtualPath = ApplicationPart.ProcessVirtualPath(GetType().Assembly, VirtualPath, virtualPath);
                    return base.Href(virtualPath, pathParts);
                }");

            generatedClass.Members.Add(hrefMethod);

            // If the generatedClass starts with an underscore, add a ClsCompliant(false) attribute.
            if (generatedClass.Name.StartsWith("_", StringComparison.OrdinalIgnoreCase)) {
                generatedClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(CLSCompliantAttribute).FullName,
                                                        new CodeAttributeArgument(new CodePrimitiveExpression(false))));
            }
        }
    }
}
