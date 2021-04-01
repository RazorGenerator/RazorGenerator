using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace RazorGenerator.Core.CodeTransformers
{
    public class SetImports : RazorCodeTransformerBase
    {
        private readonly IEnumerable<string> _imports;
        private readonly bool _replaceExisting;

        public SetImports(IEnumerable<string> imports, bool replaceExisting = false)
        {
            this._imports = imports;
            this._replaceExisting = replaceExisting;
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            if (this._replaceExisting)
            {
                razorHost.NamespaceImports.Clear();
            }
            foreach (string import in this._imports)
            {
                razorHost.NamespaceImports.Add(import);
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            // Sort imports.
            List<CodeNamespaceImport> imports = new List<CodeNamespaceImport>(generatedNamespace.Imports.OfType<CodeNamespaceImport>());
            generatedNamespace.Imports.Clear();
            generatedNamespace.Imports.AddRange(imports.OrderBy(c => c.Namespace, NamespaceComparer.Instance).ToArray());
        }

        private class NamespaceComparer : IComparer<string>
        {
            public static readonly NamespaceComparer Instance = new NamespaceComparer();
            public int Compare(string x, string y)
            {
                if (x == null || y == null)
                {
                    return StringComparer.OrdinalIgnoreCase.Compare(x, y);
                }
                bool xIsSystem = x.StartsWith("System", StringComparison.OrdinalIgnoreCase);
                bool yIsSystem = y.StartsWith("System", StringComparison.OrdinalIgnoreCase);

                if (!(xIsSystem ^ yIsSystem))
                {
                    return x.CompareTo(y);
                }
                else if (xIsSystem)
                {
                    return -1;
                }
                return 1;
            }
        }
    }
}
