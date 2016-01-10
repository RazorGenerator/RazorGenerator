﻿using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public class RazorCodeTransformerBase : IRazorCodeTransformer
    {

        protected RazorHost _razorHost;

        void IRazorCodeTransformer.Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            Initialize((RazorHost)razorHost, directives);
        }

        public virtual void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            _razorHost = razorHost;
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
