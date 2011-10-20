using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public abstract class AggregateCodeTransformer : IRazorCodeTransformer
    {

        protected abstract IEnumerable<IRazorCodeTransformer> CodeTransformers
        {
            get;
        }

        public virtual void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            foreach (var transformer in CodeTransformers)
            {
                transformer.Initialize(razorHost, directives);
            }
        }

        public virtual void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            foreach (var transformer in CodeTransformers)
            {
                transformer.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            }
        }

        public virtual string ProcessOutput(string codeContent)
        {
            foreach (var transformer in CodeTransformers)
            {
                codeContent = transformer.ProcessOutput(codeContent);
            }
            return codeContent;
        }
    }
}
