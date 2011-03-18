using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.WebPages.Razor;

namespace Microsoft.Web.RazorSingleFileGenerator {
    public class CompiledWebHelperRazorHost : WebCodeRazorHost {
        public CompiledWebHelperRazorHost(string fileNamespace, string projectRelativePath, string fullPath)
            : base(GetVirtualPath(projectRelativePath), fullPath) {
            DefaultNamespace = String.IsNullOrEmpty(fileNamespace) ? "ASP" : fileNamespace;
        }

        protected override string GetClassName(string virtualPath) {
            return Path.GetFileNameWithoutExtension(virtualPath);
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

            // Since we do not have a way to pass additional metadata to post-process a Razor file, we use a simple 
            // naming convention to determine the visibility of types and methods as all types and helper methods 
            // are public by default.

            if (generatedClass.Name.StartsWith("_")) {
                // If a class name starts with _ make it internal and remove the leading '_'. 
                generatedClass.TypeAttributes ^= TypeAttributes.Public | TypeAttributes.NestedFamORAssem;
                generatedClass.Name = generatedClass.Name.Substring(1);
            }

            // Make all helper methods prefixed by '_' internal
            foreach (var method in generatedClass.Members.OfType<CodeSnippetTypeMember>()) {
                method.Text = Regex.Replace(method.Text, "public static System\\.Web\\.WebPages\\.HelperResult _",
                     "internal static System.Web.WebPages.HelperResult _");
            }

            // Mark the result as generated code
            generatedClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(GeneratedCodeAttribute).FullName,
                    new CodeAttributeArgument(new CodePrimitiveExpression("RazorSingleFileGenerator")),
                    new CodeAttributeArgument(new CodePrimitiveExpression(typeof(CompiledWebRazorHost).Assembly.GetName().Version.ToString()))));
        }
    }
}
