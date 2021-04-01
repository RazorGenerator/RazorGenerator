using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    /// <summary>This transformer adds <c>[System.Web.WebPages.PageVirtualPath($virtualPath)]</c> to the generated class, where <c>$virtualPath</c> is specified by the RazorGenerator directive &quot;<c>VirtualPath</c>&quot; - otherwise it uses the project-relative path of the .cshtml/.vbhtml file.</summary>
    public class AddPageVirtualPathAttribute : RazorCodeTransformerBase
    {
        private const string VirtualPathDirectiveKey = "VirtualPath";

        private string _projectRelativePath;
        private string _overriddenVirtualPath;
        private IRazorVirtualPathUtility _virtualPathUtility;

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            this._projectRelativePath = razorHost.ProjectRelativePath;
            _ = directives.TryGetValue(VirtualPathDirectiveKey, out this._overriddenVirtualPath);
            this._virtualPathUtility = razorHost.GetVirtualPathUtility();
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            Debug.Assert(this._projectRelativePath != null, "Initialize has to be called before we get here.");
            
            if( this._virtualPathUtility is null ) return; // TODO: How to return warning indicating that VirtualPaths aren't supported?

            string virtualPath;
            try
            {
                if( this._overriddenVirtualPath != null )
                {
                    virtualPath = this._overriddenVirtualPath;
                }
                else
                {
                    string relativeTrimmed = this._projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    virtualPath = this._virtualPathUtility.ToAppRelative("~/" + relativeTrimmed);
                }
            }
            catch (Exception)
            {
                // Crap mono.
                virtualPath = "~/" + this._projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            if( this._virtualPathUtility.TryGetVirtualPathAttribute(virtualPath, out CodeAttributeDeclaration attribute) )
            {
                _ = generatedClass.CustomAttributes.Add( attribute );
            }
        }
    }
}
