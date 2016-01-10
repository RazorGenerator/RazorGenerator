using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorGenerator.Core
{
    abstract public class CodeLanguageUtil
    {

        #region "Language Util Factory"
        public static CodeLanguageUtil GetLanguageUtilFromFileExtension(string extension)
        {
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }
            extension = extension.ToLower();

            switch (extension)
            {
                case "cshtml":
                    return new CodeLanguageUtils.CSharpCodeLanguageUtil();
                case "vbhtml":
                    return new CodeLanguageUtils.VBCodeLanguageUtil();
                default:
                    throw new ArgumentException("Extension " + extension + " is not supported");
            }
        }

        public static CodeLanguageUtil GetLanguageUtilFromFileName(string filename)
        {
            return GetLanguageUtilFromFileExtension(System.IO.Path.GetExtension(filename));
        }
        #endregion


        abstract public string BuildGenericTypeReference(string GenericType, IEnumerable<string> GenericArguments);
        abstract public bool IsGenericTypeReference(string TypeName);
        abstract public string GetCodeFileExtension();
        abstract public string GetPreGeneratedCodeBlock();
        abstract public string GetPostGeneratedCodeBlock();
        abstract public System.CodeDom.Compiler.CodeDomProvider GetCodeDomProvider();
        abstract public string DefaultModelTypeName { get; }
        abstract public string MakeTypeStatic(string codeContent);
        abstract public string MakeHelperMethodsInternal(string methodText);
        abstract public string HrefMethod { get; }
    }
}
