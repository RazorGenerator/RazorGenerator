using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
            return GetLanguageUtilFromFileExtension(Path.GetExtension(filename));
        }
        #endregion


        public abstract string BuildGenericTypeReference(string GenericType, IEnumerable<string> GenericArguments);
        public abstract bool IsGenericTypeReference(string TypeName);
        public abstract string GetCodeFileExtension();
        public abstract string GetProjectFileExtension();
        public abstract string GetPreGeneratedCodeBlock();
        public abstract string GetPostGeneratedCodeBlock();
        public abstract System.CodeDom.Compiler.CodeDomProvider GetCodeDomProvider();
        public abstract string DefaultModelTypeName { get; }
        public abstract string MakeTypeStatic(string codeContent);
        public abstract string MakeHelperMethodsInternal(string methodText);
        public abstract string HrefMethod { get; }
    }
}
