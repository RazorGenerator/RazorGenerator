using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.Razor.Generator;

namespace RazorGenerator.Core.CodeTransformers
{
    [Export("MvcHelper", typeof(IRazorCodeTransformer))]
    public class Version2MvcHelperTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private const string WriteToMethodName        = "WriteTo";
        private const string WriteLiteralToMethodName = "WriteLiteralTo";
        
        private readonly RazorCodeTransformerBase[] _transformers = new RazorCodeTransformerBase[] {
            new SetImports(Version2MvcViewTransformer.MvcNamespaces, replaceExisting: false),
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
            
            if( razorHost is Version2RazorHost v2RazorHost )
            {
                this.Initialize( v2RazorHost, directives );
            }
            else
            {
                string message = "Expected " + nameof(razorHost) + " to be an instance of " + nameof(Version2RazorHost) + " but encountered an instance of " + razorHost.GetType().FullName + ".";
                throw new InvalidOperationException( message );
            }
        }

        public void Initialize(Version2RazorHost razorHost, IDictionary<string,string> directives)
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
