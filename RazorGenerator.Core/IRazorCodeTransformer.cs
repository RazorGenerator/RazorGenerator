using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public interface IRazorCodeTransformer
    {
        void Initialize(IRazorHost razorHost, IDictionary<string, string> directives);

        void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod);

        string ProcessOutput(string codeContent);
    }
}
