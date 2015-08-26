using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace RazorGenerator.Core
{
    public class GenericParametersTransformer : RazorCodeTransformerBase
    {
        readonly string[] _parameters;

        public GenericParametersTransformer(IEnumerable<string> parameters)
        {
            _parameters = parameters.ToArray(/* copy */);
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            var parameters = from p in _parameters select new CodeTypeParameter(p);
            generatedClass.TypeParameters.AddRange(parameters.ToArray());
        }
    }
}
