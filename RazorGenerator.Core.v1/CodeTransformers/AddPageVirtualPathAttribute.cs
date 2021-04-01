using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace RazorGenerator.Core
{
    public class AddPageVirtualPathAttribute : RazorCodeTransformerBase
    {
        private const string VirtualPathDirectiveKey = "VirtualPath";
        private string _projectRelativePath;
        private string _overriddenVirtualPath;

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            this._projectRelativePath = razorHost.ProjectRelativePath;
            directives.TryGetValue(VirtualPathDirectiveKey, out this._overriddenVirtualPath);
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            Debug.Assert(this._projectRelativePath != null, "Initialize has to be called before we get here.");
            string virtualPath;
            try
            {
                virtualPath = this._overriddenVirtualPath ?? VirtualPathUtility.ToAppRelative("~/" + this._projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch (HttpException)
            {
                // Crap mono.
                virtualPath = "~/" + this._projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(typeof(System.Web.WebPages.PageVirtualPathAttribute).FullName,
                new CodeAttributeArgument(new CodePrimitiveExpression(virtualPath))));
        }
    }
}
