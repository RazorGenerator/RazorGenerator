using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorGenerator.Core
{
    public class SuffixFileNameTransformer : RazorCodeTransformerBase
    {
        private readonly string _suffix;

        public SuffixFileNameTransformer(string suffix)
        {
            this._suffix = suffix;
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            if (!String.IsNullOrEmpty(this._suffix))
            {
                generatedClass.Name += this._suffix;
            }
        }
    }
}
