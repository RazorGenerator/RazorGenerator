using System;
using System.CodeDom;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.WebPages.Razor;

namespace Microsoft.Web.RazorSingleFileGenerator {
    public class CompiledWebRazorHost : WebPageRazorHost {


        public CompiledWebRazorHost(string fileNamespace, string projectRelativePath, string fullPath)
            : base(GetVirtualPath(projectRelativePath), fullPath) {
            DefaultNamespace = fileNamespace;
        }

        protected override string GetClassName(string virtualPath) {
            virtualPath = virtualPath.TrimStart('~', '/');
            return Regex.Replace(virtualPath, @"[\/.]", "_");
        }

        private static string GetVirtualPath(string projectRelativePath) {
            return VirtualPathUtility.ToAppRelative("~" + projectRelativePath);
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit,
                                                      CodeNamespace generatedNamespace,
                                                      CodeTypeDeclaration generatedClass,
                                                      CodeMemberMethod executeMethod) {

            // Run the base processing
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            if (!IsSpecialPage) {


                // Create the Href wrapper
                CodeTypeMember hrefMethod = new CodeSnippetTypeMember(String.Format(@"
                    // Resolve package relative syntax
                    // Also, if it comes from a static embedded resource, change the path accordingly
                    public override string Href(string virtualPath, params object[] pathParts) {{
                        virtualPath = ApplicationPart.ProcessVirtualPath(typeof({0}).Assembly, VirtualPath, virtualPath);
                        return base.Href(virtualPath, pathParts);
                    }}
                    ", generatedClass.Name));

                generatedClass.Members.Add(hrefMethod);
            }

            // Add a PageVirtualPathAttribute attribute.
            // TODO: clean this up.  Use Type instead of string once it makes it into Plan9
            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration("PageVirtualPathAttribute",
                    new CodeAttributeArgument(new CodePrimitiveExpression(VirtualPath))));
        }
    }
}
