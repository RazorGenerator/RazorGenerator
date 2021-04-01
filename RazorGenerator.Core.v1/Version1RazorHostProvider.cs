using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core.Mvc3
{
    [Export(typeof(IRazorHostProvider))]
    public class Version1RazorHostProvider : IRazorHostProvider
    {
        public IRazorHost GetRazorHost(
            string                     projectRelativePath,
            FileInfo                   fullPath,
            IRazorCodeTransformer      codeTransformer,
            CodeDomProvider            codeDomProvider,
            IDictionary<string,string> directives
        )
        {
            return new Version1RazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
        }
    }
}
