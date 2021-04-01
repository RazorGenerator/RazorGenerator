using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    [Export("Template", typeof(IOutputRazorCodeTransformer))]
    public class Version2TemplateCodeTransformer : TemplateCodeTransformer, IOutputRazorCodeTransformer
    {
    }
}
