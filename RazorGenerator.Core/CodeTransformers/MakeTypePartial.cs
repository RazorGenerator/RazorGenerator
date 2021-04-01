using System.CodeDom;

namespace RazorGenerator.Core.CodeTransformers
{
    public class MakeTypePartial : RazorCodeTransformerBase
    {
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            generatedClass.IsPartial = true;
        }
    }
}
