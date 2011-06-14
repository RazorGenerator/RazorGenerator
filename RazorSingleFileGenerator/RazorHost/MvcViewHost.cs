using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Web.Mvc.Razor;
using System.Web.Razor.Parser;
using System.Web.WebPages;

namespace RazorGenerator.RazorHost {
    [Export("MvcView", typeof(ISingleFileGenerator))]
    public class MvcViewHost : MvcWebPageRazorHost, ISingleFileGenerator {

        [ImportingConstructor]
        public MvcViewHost(
                   [Import("fileNamespace")] string fileNamespace,
                   [Import("projectRelativePath")] string projectRelativePath,
                   [Import("fullPath")] string fullPath)
            : base(HostHelper.GetVirtualPath(projectRelativePath), fullPath) {

            DefaultNamespace = fileNamespace;
        }

        public void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives) {
            // Do nothing
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod) {
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            generatedNamespace.Imports.Add(new CodeNamespaceImport("System.Web.Mvc"));
            generatedNamespace.Imports.Add(new CodeNamespaceImport("System.Web.Mvc.Html"));

            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(typeof(PageVirtualPathAttribute).FullName,
                    new CodeAttributeArgument(new CodePrimitiveExpression(VirtualPath))));
        }
    }
}
