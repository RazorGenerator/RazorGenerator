using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core.CodeTransformers
{
    /// <summary>
    /// Denotes a type that is responsible for overall transformation of a Razor source file (.cshtml/.vbhtml). Implementations of these correspond to the &quot;Generator Types&quot; listed in the README.md file:
    /// <list type="bullet">
    /// <item>MvcHelper</item>
    /// <item>MvcView</item>
    /// <item>WebPage</item>
    /// <item>WebPagesHelper</item>
    /// <item>Template</item>
    /// </list>
    /// </summary>
    public interface IOutputRazorCodeTransformer : IRazorCodeTransformer
    {
    }
}
