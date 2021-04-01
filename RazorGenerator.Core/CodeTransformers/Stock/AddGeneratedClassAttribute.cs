using System;
using System.CodeDom;

namespace RazorGenerator.Core.CodeTransformers
{
    public class AddGeneratedClassAttribute : RazorCodeTransformerBase
    {
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            Version version = this.GetType().Assembly.GetName().Version;
            
            _ = generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(
                    typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName,
                    new CodeAttributeArgument(new CodePrimitiveExpression("RazorGenerator")),
                    new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString()))
                )
            );
        }
    }
}
