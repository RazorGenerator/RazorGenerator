using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.Razor.Generator;

namespace RazorGenerator.Core.CodeTransformers
{
    [Export("MvcHelper", typeof(IOutputRazorCodeTransformer))]
    public class Version3MvcHelperTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private const string WriteToMethodName        = "WriteTo";
        private const string WriteLiteralToMethodName = "WriteLiteralTo";
        
        private readonly RazorCodeTransformerBase[] _transformers = new RazorCodeTransformerBase[] {
            new SetImports(Version3MvcViewTransformer.MvcNamespaces, replaceExisting: false),
            new AddGeneratedClassAttribute(),
            new DirectivesBasedTransformers(),
            new MakeTypeHelper(),
            new RemoveLineHiddenPragmas(),
            new MvcWebConfigTransformer(),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return this._transformers; }
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string,string> directives)
        {
            if (razorHost  is null) throw new ArgumentNullException(nameof(razorHost));
            if (directives is null) throw new ArgumentNullException(nameof(directives));

            base.Initialize(razorHost, directives);
            
            if( razorHost is Version3RazorHost v3RazorHost )
            {
                this.Initialize( v3RazorHost );
            }
            else
            {
                string message = "Expected " + nameof(razorHost) + " to be an instance of " + nameof(Version3RazorHost) + " but encountered an instance of " + razorHost.GetType().FullName + ".";
                throw new InvalidOperationException( message );
            }
        }

        private void Initialize(Version3RazorHost razorHost)
        {
            razorHost.DefaultBaseClass = typeof(System.Web.WebPages.HelperPage).FullName;

            razorHost.GeneratedClassContext = new GeneratedClassContext(
                executeMethodName       : GeneratedClassContext.DefaultExecuteMethodName,
                writeMethodName         : GeneratedClassContext.DefaultWriteMethodName,
                writeLiteralMethodName  : GeneratedClassContext.DefaultWriteLiteralMethodName,
                writeToMethodName       : WriteToMethodName,
                writeLiteralToMethodName: WriteLiteralToMethodName,
                templateTypeName        : typeof(System.Web.WebPages.HelperResult).FullName,
                defineSectionMethodName : "DefineSection"
            );
        }

        public override void ProcessGeneratedCode(
            CodeCompileUnit     codeCompileUnit,
            CodeNamespace       generatedNamespace,
            CodeTypeDeclaration generatedClass,
            CodeMemberMethod    executeMethod
        )
        {

            // Run the base processing
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            // Remove the constructor 
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
        }
    }
}