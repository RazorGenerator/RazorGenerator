using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;

namespace RazorGenerator.Core.CodeTransformers
{
    [Export("WebPage", typeof(IRazorCodeTransformer))]
    public class Version1WebPageTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private readonly List<RazorCodeTransformerBase> _transformers = new List<RazorCodeTransformerBase> { 
            new DirectivesBasedTransformers(),
            new AddGeneratedClassAttribute(),
            new AddPageVirtualPathAttribute(),
            new RemoveLineHiddenPragmas(),
            new SetImports(new[] { "System.Web.WebPages.Html" }, replaceExisting: false),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get
            {
                return this._transformers;
            }
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            base.Initialize(razorHost, directives);

            // Remove the extension and replace path separator slashes with underscores
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(razorHost.ProjectRelativePath);

            // If its a PageStart page, set the base type to StartPage.
            if (fileNameWithoutExtension.Equals("_pagestart", StringComparison.OrdinalIgnoreCase))
            {
                razorHost.DefaultBaseClass = typeof(System.Web.WebPages.StartPage).FullName;
            }
        }


        public override void ProcessGeneratedCode(
            CodeCompileUnit     codeCompileUnit,
            CodeNamespace       generatedNamespace,
            CodeTypeDeclaration generatedClass,
            CodeMemberMethod    executeMethod
        )
        {
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);


            // Create the Href wrapper
            CodeTypeMember hrefMethod = new CodeSnippetTypeMember(this.razorHost.CodeLanguageUtil.HrefMethod);

            _ = generatedClass.Members.Add(hrefMethod);

            Debug.Assert(generatedClass.Name.Length > 0);
            if (!(Char.IsLetter(generatedClass.Name[0]) || generatedClass.Name[0] == '_'))
            {
                generatedClass.Name = '_' + generatedClass.Name;
            }

            // If the generatedClass starts with an underscore, add a ClsCompliant(false) attribute.
            if (generatedClass.Name[0] == '_')
            {
                _ = generatedClass.CustomAttributes.Add(
                    new CodeAttributeDeclaration(
                        typeof(CLSCompliantAttribute).FullName,
                        new CodeAttributeArgument(new CodePrimitiveExpression(false))
                    )
                );
            }
        }
    }
}
