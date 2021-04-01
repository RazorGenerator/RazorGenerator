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
            this._parameters = parameters.ToArray(/* copy */);
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            IEnumerable<CodeTypeParameter> parameters = from p in this._parameters select new CodeTypeParameter(p);
            generatedClass.TypeParameters.AddRange(parameters.ToArray());
        }
    }
}
