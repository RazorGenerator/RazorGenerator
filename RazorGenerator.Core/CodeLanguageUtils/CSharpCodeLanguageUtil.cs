using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core.CodeLanguageUtils
{
    public class CSharpCodeLanguageUtil : CodeLanguageUtil
    {
        public override string DefaultModelTypeName
        {
            get
            {
                return "dynamic";
            }
        }

        public override string BuildGenericTypeReference(string genericType, IEnumerable<string> genericArguments)
        {
            StringBuilder ret = new StringBuilder();
#pragma warning disable IDE0058 // Expression value is never used

            ret.Append(genericType);
            ret.Append("<");
            bool first = true;
            foreach (string ga in genericArguments)
            {
                if (!first)
                {
                    ret.Append(", ");
                }
                ret.Append(ga);
            }
            ret.Append(">");
            return ret.ToString();

#pragma warning restore
        }

        public override CodeDomProvider GetCodeDomProvider()
        {
            return new Microsoft.CSharp.CSharpCodeProvider();
        }

        public override string GetCodeFileExtension()
        {
            return ".cs";
        }

        public override string GetPostGeneratedCodeBlock()
        {
            return "#pragma warning restore 1591";
        }

        public override string GetPreGeneratedCodeBlock()
        {
            return "#pragma warning disable 1591";
        }

        public override string HrefMethod
        {
            get
            {
                return @"
                // Resolve package relative syntax
                // Also, if it comes from a static embedded resource, change the path accordingly
                public override string Href(string virtualPath, params object[] pathParts) {
                    virtualPath = ApplicationPart.ProcessVirtualPath(GetType().Assembly, this.VirtualPath, virtualPath);
                    return base.Href(virtualPath, pathParts);
                }";
            }
        }

        public override bool IsGenericTypeReference(string typeName)
        {
            return typeName.Contains("<");
        }

        public override string MakeHelperMethodsInternal(string methodText)
        {
            const string replacement = "internal static System.Web.WebPages.HelperResult _";
            return Regex.Replace(methodText, @"public static System\.Web\.WebPages\.HelperResult _", replacement);
        }

        public override string MakeTypeStatic(string codeContent)
        {
            return codeContent.Replace("public class", "public static class");
        }
    }
}
