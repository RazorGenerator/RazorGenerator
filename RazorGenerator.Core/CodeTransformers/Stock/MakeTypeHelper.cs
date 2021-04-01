using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core.CodeTransformers
{
    public class MakeTypeHelper : RazorCodeTransformerBase
    {
        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            razorHost.StaticHelpers = true;
        }
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            generatedClass.Members.Remove(executeMethod);
        }
    }
}
