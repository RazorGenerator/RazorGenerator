using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
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

        public override string HrefMethod
        {
            get
            {
                return @"
                // Resolve package relative syntax
                // Also, if it comes from a static embedded resource, change the path accordingly
                public override string Href(string virtualPath, params object[] pathParts) {
                    virtualPath = ApplicationPart.ProcessVirtualPath(GetType().Assembly, VirtualPath, virtualPath);
                    return base.Href(virtualPath, pathParts);
                }";
            }
        }

        public override string BuildGenericTypeReference(string GenericType, IEnumerable<string> GenericArguments)
        {
            StringBuilder ret = new StringBuilder();
            ret.Append(GenericType);
            ret.Append("<");
            bool first = true;
            foreach (string ga in GenericArguments)
            {
                if (!first)
                {
                    ret.Append(", ");
                }
                ret.Append(ga);
            }
            ret.Append(">");
            return ret.ToString();
        }

        public override CodeDomProvider GetCodeDomProvider()
        {
            return new Microsoft.CSharp.CSharpCodeProvider();
        }

        public override string GetCodeFileExtension()
        {
            return ".cs";
        }

        public override string GetProjectFileExtension()
        {
            return ".csproj";
        }

        public override string GetPostGeneratedCodeBlock()
        {
            return "#pragma warning restore 1591";
        }

        public override string GetPreGeneratedCodeBlock()
        {
            return "#pragma warning disable 1591";
        }

        public override bool IsGenericTypeReference(string TypeName)
        {
            return TypeName.Contains("<");
        }

        public override string MakeHelperMethodsInternal(string methodText)
        {
            return Regex.Replace(methodText, "public static System\\.Web\\.WebPages\\.HelperResult _",
                     "internal static System.Web.WebPages.HelperResult _");
        }

        public override string MakeTypeStatic(string codeContent)
        {
            return codeContent.Replace("public class", "public static class");
        }
    }
}
