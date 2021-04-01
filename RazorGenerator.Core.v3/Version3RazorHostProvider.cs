using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    [Export(typeof(IRazorHostProvider))]
    public class Version3RazorHostProvider : IRazorHostProvider
    {
        public IRazorHost GetRazorHost(
            string                     projectRelativePath,
            FileInfo                   fullPath,
            IRazorCodeTransformer      codeTransformer,
            CodeDomProvider            codeDomProvider,
            IDictionary<string,string> directives
        )
        {
            return new Version3RazorHost(projectRelativePath, fullPath, codeTransformer, codeDomProvider, directives);
        }
    }
}
