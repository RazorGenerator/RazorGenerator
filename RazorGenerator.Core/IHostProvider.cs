using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public interface IHostProvider
    {
        IRazorHost GetRazorHost(string projectRelativePath, 
                                string fullPath, 
                                IRazorCodeTransformer codeTransformer, 
                                CodeDomProvider codeDomProvider, 
                                IDictionary<string, string> directives);
    }
}
