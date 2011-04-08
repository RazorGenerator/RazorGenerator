using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.WebPages;
using System.Web.WebPages.Razor;

namespace Microsoft.Web.RazorSingleFileGenerator.RazorHost {
    public class WebPageHost : WebPageRazorHost, ISingleFileGenerator {
        public WebPageHost(string fileNamespace, string projectRelativePath, string fullPath)
            : base(GetVirtualPath(projectRelativePath), fullPath) {

            DefaultNamespace = fileNamespace;
        }

        protected static string GetVirtualPath(string projectRelativePath) {
            return VirtualPathUtility.ToAppRelative("~" + projectRelativePath);
        }

        protected override string GetClassName(string virtualPath) {
            virtualPath = virtualPath.TrimStart('~', '/', '_');
            return Regex.Replace(virtualPath, @"[\/.]", "_");
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit,
                                                      CodeNamespace generatedNamespace,
                                                      CodeTypeDeclaration generatedClass,
                                                      CodeMemberMethod executeMethod) {

            // Run the base processing
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            if (!IsSpecialPage || IsPageStart(generatedClass)) {

                // Create the Href wrapper
                CodeTypeMember hrefMethod = new CodeSnippetTypeMember(@"
                    // Resolve package relative syntax
                    // Also, if it comes from a static embedded resource, change the path accordingly
                    public override string Href(string virtualPath, params object[] pathParts) {
                        virtualPath = ApplicationPart.ProcessVirtualPath(GetType().Assembly, VirtualPath, virtualPath);
                        return base.Href(virtualPath, pathParts);
                    }");

                generatedClass.Members.Add(hrefMethod);
            }

            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(typeof(PageVirtualPathAttribute).FullName,
                    new CodeAttributeArgument(new CodePrimitiveExpression(VirtualPath))));

            generatedClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(GeneratedCodeAttribute).FullName,
                    new CodeAttributeArgument(new CodePrimitiveExpression("RazorSingleFileGenerator")),
                    new CodeAttributeArgument(new CodePrimitiveExpression(typeof(WebPageHost).Assembly.GetName().Version.ToString()))));
        }

        private bool IsPageStart(CodeTypeDeclaration generatedClass) {
            return generatedClass.BaseTypes[0].BaseType == typeof(System.Web.WebPages.StartPage).FullName;
        }

        public virtual void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives) {
            
        }
    }
}
