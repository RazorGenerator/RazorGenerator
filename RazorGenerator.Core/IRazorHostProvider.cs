using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    public interface IRazorHostProvider
    {
        IRazorHost GetRazorHost(
            string                     projectRelativePath, 
            FileInfo                   fullPath, 
            IRazorCodeTransformer      codeTransformer, // Question: Why is this a *singular* `IRazorCodeTransformer`? I thought there are multiple...
            CodeDomProvider            codeDomProvider, 
            IDictionary<string,string> directives
        );
    }
}
