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
            _suffix = suffix;
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            if (!String.IsNullOrEmpty(_suffix))
            {
                generatedClass.Name += _suffix;
            }
        }
    }
}
