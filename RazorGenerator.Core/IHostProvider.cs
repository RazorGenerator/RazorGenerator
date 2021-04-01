using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    public interface IHostProvider
    {
        IRazorHost GetRazorHost(
            string                     projectRelativePath, 
            FileInfo                   fullPath, 
            IRazorCodeTransformer      codeTransformer, 
            CodeDomProvider            codeDomProvider, 
            IDictionary<string,string> directives
        );
    }
}
