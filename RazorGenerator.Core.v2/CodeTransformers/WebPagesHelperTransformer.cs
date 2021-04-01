using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core.CodeTransformers
{
    [Export("WebPagesHelper", typeof(IRazorCodeTransformer))]
    public class Version2WebPagesHelperTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private readonly RazorCodeTransformerBase[] _codeTransformers = new RazorCodeTransformerBase[] {
            new DirectivesBasedTransformers(),
            new AddGeneratedClassAttribute(),
            new SetImports(new[] { "System.Web.WebPages.Html" }, replaceExisting: false),
            new SetBaseType(typeof(System.Web.WebPages.HelperPage)),
            new MakeTypeHelper(),
            new RemoveLineHiddenPragmas(),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return this._codeTransformers; }
        }

        public override void ProcessGeneratedCode(
            CodeCompileUnit     codeCompileUnit,
            CodeNamespace       generatedNamespace,
            CodeTypeDeclaration generatedClass,
            CodeMemberMethod    executeMethod
        )
        {
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            // Make all helper methods prefixed by '_' internal
            foreach (CodeSnippetTypeMember method in generatedClass.Members.OfType<CodeSnippetTypeMember>())
            {
                method.Text = this.razorHost.CodeLanguageUtil.MakeHelperMethodsInternal(method.Text);
            }
        }
    }
}
