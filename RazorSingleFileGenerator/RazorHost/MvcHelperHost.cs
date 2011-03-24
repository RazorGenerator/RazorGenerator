using System;
using System.CodeDom;
using System.Linq;
using System.Web.Razor.Generator;

namespace Microsoft.Web.RazorSingleFileGenerator {
    public class MvcHelperHost : WebPagesHelperHost {
        private const string WriteToMethodName = "WebViewPage.WriteTo";
        private const string WriteLiteralToMethodName = "WebViewPage.WriteLiteralTo";

        public MvcHelperHost(string fileNamespace, string projectRelativePath, string fullPath)
            : base(fileNamespace, projectRelativePath, fullPath) {
            // Do not derive from HelperPage.
            DefaultBaseClass = String.Empty;

            GenerateStaticType = true;

            // Replace the WriteTo and WriteLiteralTo methods 
            GeneratedClassContext = new GeneratedClassContext(base.GeneratedClassContext.ExecuteMethodName, 
                base.GeneratedClassContext.WriteMethodName, base.GeneratedClassContext.WriteLiteralMethodName, 
                WriteToMethodName, WriteLiteralToMethodName, base.GeneratedClassContext.TemplateTypeName);
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit,
                                                      CodeNamespace generatedNamespace,
                                                      CodeTypeDeclaration generatedClass,
                                                      CodeMemberMethod executeMethod) {

            // Run the base processing
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            var ns = codeCompileUnit.Namespaces[0];
            // Remove all Plan9 related namespaces
            var imports = ns.Imports.OfType<CodeNamespaceImport>().Where(KeepImport).ToList();
            ns.Imports.Clear();
            imports.ForEach(import => ns.Imports.Add(import));
            ns.Imports.Add(new CodeNamespaceImport("System.Web.Mvc"));
            ns.Imports.Add(new CodeNamespaceImport("System.Web.Mvc.Html"));

            // Remove the constructor 
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeMemberProperty>().SingleOrDefault());
        }

        private static bool KeepImport(CodeNamespaceImport import) {
            // Remove all the WebMatrix namespaces
            if (import.Namespace.IndexOf("WebMatrix", StringComparison.OrdinalIgnoreCase) != -1) {
                return false;
            }

            // System.Web.WebPages.Html conflicts with some MVC namespaces, so remove it
            if (import.Namespace == typeof(System.Web.WebPages.Html.HtmlHelper).Namespace) {
                return false;
            }

            return true;
        }
    }
}
