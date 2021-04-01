using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core.CodeTransformers
{
    public class RazorCodeTransformerBase : IRazorCodeTransformer
    {
        protected IRazorHost razorHost;

        void IRazorCodeTransformer.Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            this.Initialize(razorHost, directives);
        }

        public virtual void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            this.razorHost = razorHost;
            // do nothing
        }

        public virtual void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            // do nothing.
        }

        public virtual string ProcessOutput(string codeContent)
        {
            return codeContent;
        }
    }
}
