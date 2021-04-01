using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.Razor.Generator;

namespace RazorGenerator.Core.CodeTransformers
{
    [Export("MvcHelper", typeof(IOutputRazorCodeTransformer))]
    public class Version1MvcHelperTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private const string WriteToMethodName        = "WebViewPage.WriteTo";
        private const string WriteLiteralToMethodName = "WebViewPage.WriteLiteralTo";

        private static readonly RazorCodeTransformerBase[] _transformers = new RazorCodeTransformerBase[] {
            new SetImports(Version1MvcViewTransformer.MvcNamespaces, replaceExisting: false),
            new AddGeneratedClassAttribute(),
            new DirectivesBasedTransformers(),
            new MakeTypeStatic(),
            new MakeTypeHelper(),
            new RemoveLineHiddenPragmas(),
            new MvcWebConfigTransformer(),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _transformers; }
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string,string> directives)
        {
            if (razorHost  is null) throw new ArgumentNullException(nameof(razorHost));
            if (directives is null) throw new ArgumentNullException(nameof(directives));

            if( razorHost is Version1RazorHost v1RazorHost )
            {
                base.Initialize( razorHost, directives );

                this.Initialize( v1RazorHost );
            }
            else
            {
                string message = "Expected " + nameof(razorHost) + " to be an instance of " + nameof(Version1RazorHost) + " but encountered an instance of " + razorHost.GetType().FullName + ".";
                throw new InvalidOperationException( message );
            }
        }

        private void Initialize(Version1RazorHost razorHost)
        {
            razorHost.GeneratedClassContext = new GeneratedClassContext(
                executeMethodName       : GeneratedClassContext.DefaultExecuteMethodName,
                writeMethodName         : GeneratedClassContext.DefaultWriteMethodName,
                writeLiteralMethodName  : GeneratedClassContext.DefaultWriteLiteralMethodName,
                writeToMethodName       : WriteToMethodName,
                writeLiteralToMethodName: WriteLiteralToMethodName,
                templateTypeName        : typeof(System.Web.WebPages.HelperResult).FullName,
                defineSectionMethodName : "DefineSection"
            );

            razorHost.DefaultBaseClass = String.Empty;
        }

        public override void ProcessGeneratedCode(
            CodeCompileUnit     codeCompileUnit,
            CodeNamespace       generatedNamespace,
            CodeTypeDeclaration generatedClass,
            CodeMemberMethod    executeMethod)
        {

            // Run the base processing
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            // Remove the constructor 
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
        }
    }
}
