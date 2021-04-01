using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RazorGenerator.Core
{
    public abstract class CodeLanguageUtil
    {
        public static CodeLanguageUtil GetLanguageUtilFromFileExtension(string extension)
        {
            if (extension is null) throw new ArgumentNullException(nameof(extension));

            if ("cshtml".Equals(extension, StringComparison.OrdinalIgnoreCase) || ".cshtml".Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return new CodeLanguageUtils.CSharpCodeLanguageUtil();
            }
            else if ("vbhtml".Equals(extension, StringComparison.OrdinalIgnoreCase) || ".vbhtml".Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return new CodeLanguageUtils.VBCodeLanguageUtil();
            }
            else
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(extension), actualValue: extension, message: "Unsupported Razor file name extension \"" + extension + "\". Only .cshtml and .vbhtml extensions are supported.");
            }
        }

        public static CodeLanguageUtil GetLanguageUtilFromFileName(FileInfo filename)
        {
            return GetLanguageUtilFromFileExtension(filename.Extension);
        }

        public abstract string          BuildGenericTypeReference(string genericType, IEnumerable<string> genericArguments);
        public abstract bool            IsGenericTypeReference(string typeName);
        public abstract string          GetCodeFileExtension();
        public abstract string          GetPreGeneratedCodeBlock();
        public abstract string          GetPostGeneratedCodeBlock();
        public abstract CodeDomProvider GetCodeDomProvider();
        public abstract string          MakeTypeStatic(string codeContent);
        public abstract string          MakeHelperMethodsInternal(string methodText);

        public abstract string          DefaultModelTypeName             { get; }
        public abstract string          HrefMethod                       { get; }
    }
}
