using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core {
    public class RazorCodeTransformerBase : IRazorCodeTransformer {
        public virtual void Initialize(RazorHost razorHost, IDictionary<string, string> directives) {
            // do nothing
        }

        public virtual void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod) {
            // do nothing.
        }

        public virtual string ProcessOutput(string codeContent) {
            return codeContent;
        }
    }
}
