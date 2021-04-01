using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    public interface IRazorHostProvider
    {
        IRazorHost GetRazorHost(
            string                      projectRelativePath, 
            FileInfo                    fullPath, 
            IOutputRazorCodeTransformer codeTransformer,
            CodeDomProvider             codeDomProvider, 
            IDictionary<string,string>  directives
        );
    }
}
