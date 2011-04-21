using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Razor.Parser;
using System.Web.WebPages.Razor;

namespace Microsoft.Web.RazorSingleFileGenerator.RazorHost
{
    public class BasicHost : WebPageRazorHost, ISingleFileGenerator
    {
        public static readonly IEnumerable<string> ExcludedNamespaces = new[] {
                "System.Web.Helpers",
                "System.Web.WebPages",
                "System.Web.WebPages.Html",
            };


        public BasicHost(string fileNamespace, string projectRelativePath, string fullPath)
            : base(GetVirtualPath(projectRelativePath), fullPath)
        {
            DefaultDebugCompilation = false;
            DefaultNamespace = fileNamespace;
            StaticHelpers = true;
        }


        public virtual void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives)
        {
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            RemoveExcludedNamespaces(generatedNamespace);

            RemoveApplicationInstanceProperty(generatedClass);
        }

        protected override string GetClassName(string virtualPath)
        {
            string filename = Path.GetFileNameWithoutExtension(virtualPath);
            return ParserHelpers.SanitizeClassName(filename);
        }

        protected static string GetVirtualPath(string projectRelativePath)
        {
            return VirtualPathUtility.ToAppRelative("~" + projectRelativePath);
        }

        protected virtual void RemoveApplicationInstanceProperty(CodeTypeDeclaration generatedClass)
        {
            CodeMemberProperty applicationInstanceProperty =
                generatedClass.Members.OfType<CodeMemberProperty>().SingleOrDefault(
                    p => "ApplicationInstance".Equals(p.Name));

            if (applicationInstanceProperty != null)
            {
                generatedClass.Members.Remove(applicationInstanceProperty);
            }
        }

        protected virtual void RemoveExcludedNamespaces(CodeNamespace codeNamespace)
        {
            List<CodeNamespaceImport> imports =
                codeNamespace.Imports.OfType<CodeNamespaceImport>()
                    .Where(import => !ExcludedNamespaces.Contains(import.Namespace))
                    .ToList();

            codeNamespace.Imports.Clear();

            imports.ForEach(import => codeNamespace.Imports.Add(import));
        }
    }
}
