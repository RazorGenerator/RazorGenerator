﻿using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace RazorGenerator.Core
{
    public class AddPageVirtualPathAttribute : RazorCodeTransformerBase
    {
        private const string VirtualPathDirectiveKey = "VirtualPath";
        private const string MvcAreaNameDirectiveKey = "CustomAreaName";
        private string _projectRelativePath;
        private string _overriddenVirtualPath;
        private string _customAreaName;

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            _projectRelativePath = razorHost.ProjectRelativePath;
            directives.TryGetValue(VirtualPathDirectiveKey, out _overriddenVirtualPath);
            if (directives.TryGetValue(MvcAreaNameDirectiveKey, out _customAreaName))
            {
                _customAreaName = string.Format("Areas/{0}/", _customAreaName);
            }
            else
            {
                _customAreaName = string.Empty;
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            Debug.Assert(_projectRelativePath != null, "Initialize has to be called before we get here.");
            string virtualPath;
            try
            {
                virtualPath = _overriddenVirtualPath ?? VirtualPathUtility.ToAppRelative("~/" + _customAreaName + _projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch (HttpException)
            {
                // Crap mono.
                virtualPath = "~/" + _projectRelativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(typeof(System.Web.WebPages.PageVirtualPathAttribute).FullName,
                new CodeAttributeArgument(new CodePrimitiveExpression(virtualPath))));
        }
    }
}
