using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core.CodeLanguageUtils
{
    public class VBCodeLanguageUtil : CodeLanguageUtil
    {
        public override string DefaultModelTypeName
        {
            get
            {
                return "Object";
            }
        }

        public override string BuildGenericTypeReference(string GenericType, IEnumerable<string> GenericArguments)
        {
            StringBuilder ret = new StringBuilder();
            ret.Append(GenericType);
            ret.Append("(Of ");
            bool first = true;
            foreach(string ga in GenericArguments)
            {
                if (!first)
                {
                    ret.Append(", ");
                }
                ret.Append(ga);
            }
            ret.Append(")");
            return ret.ToString();
        }

        public override CodeDomProvider GetCodeDomProvider()
        {
            return new Microsoft.VisualBasic.VBCodeProvider();
        }

        public override string GetCodeFileExtension()
        {
            return ".vb";
        }

        public override string GetPostGeneratedCodeBlock()
        {
            return "";
        }

        public override string GetPreGeneratedCodeBlock()
        {
            return "";
        }
        public override string HrefMethod
        {
            get
            {
                return @"
                ' Resolve package relative syntax
                ' Also, if it comes from a static embedded resource, change the path accordingly
                Public Overrides Function Href(virtualPath As String, ParamArray pathParts as Object()) As String 
                    virtualPath = ApplicationPart.ProcessVirtualPath(GetType().Assembly, VirtualPath, virtualPath)
                    return base.Href(virtualPath, pathParts)
                End Function";
            }
        }

        public override bool IsGenericTypeReference(string TypeName)
        {
            return TypeName.Contains("(Of ");
        }

        public override string MakeHelperMethodsInternal(string methodText)
        {
            return Regex.Replace(methodText, "Public Shared Function (?<a>.*?\\(.*?\\)) As System\\.Web\\.WebPages\\.HelperResult$",
                     "Friend Shared Function ${a} As System.Web.WebPages.HelperResult");
        }

        public override string MakeTypeStatic(string codeContent)
        {
            return codeContent.Replace("Public Class", "Public Module");
        }
    }
}
