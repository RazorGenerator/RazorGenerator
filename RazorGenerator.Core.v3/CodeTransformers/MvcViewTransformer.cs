using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Razor;
using System.Web.Razor.Generator;

namespace RazorGenerator.Core.CodeTransformers
{
    [Export("MvcView", typeof(IRazorCodeTransformer))]
    public class Version3MvcViewTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private const string ViewStartFileName = "_ViewStart";
        private static readonly IEnumerable<string> _namespaces = new[] { 
            "System.Web.Mvc", 
            "System.Web.Mvc.Html",
            "System.Web.Mvc.Ajax",
            "System.Web.Routing",
        };

        private readonly RazorCodeTransformerBase[] _codeTransformers = new RazorCodeTransformerBase[] { 
            new DirectivesBasedTransformers(),
            new AddGeneratedClassAttribute(),
            new AddPageVirtualPathAttribute(),
            new SetImports(_namespaces, replaceExisting: false),
            new SetBaseType(typeof(WebViewPage)),
            new RemoveLineHiddenPragmas(),
            new MvcWebConfigTransformer(),
            new MakeTypePartial(),
        };
        private bool _isSpecialPage;
        private CodeLanguageUtil _languageUtil;

        internal static IEnumerable<string> MvcNamespaces
        {
            get { return _namespaces; }
        }

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return this._codeTransformers; }
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            if (razorHost  is null) throw new ArgumentNullException(nameof(razorHost));
            if (directives is null) throw new ArgumentNullException(nameof(directives));

            base.Initialize(razorHost, directives);
            
            if( razorHost is Version3RazorHost v3Host )
            {
                this.Initialize( v3Host, directives );
            }
            else
            {
                throw new InvalidOperationException( "Expected razorHost to be an instance of " + nameof(Version3RazorHost) + " but encountered " + razorHost.GetType().FullName + "." );
            }
        }

        public void Initialize(Version3RazorHost razorHost, IDictionary<string,string> directives)
        {
            this._languageUtil = razorHost.CodeLanguageUtil;

            this._isSpecialPage = this.IsSpecialPage(razorHost.FullPath);
            this.FixupDefaultClassNameIfTemplate(razorHost);


            // The CSharpRazorCodeGenerator decides to generate line pragmas based on if the file path is available. Set it to an empty string if we 
            // do not want to generate them.
            string path = razorHost.EnableLinePragmas ? razorHost.FullPath : String.Empty;

            switch (razorHost.CodeLanguage.LanguageName)
            {
                case "csharp":
                    razorHost.CodeGenerator = new CSharpRazorCodeGenerator(razorHost.DefaultClassName, razorHost.DefaultNamespace, path, razorHost);
                    razorHost.CodeGenerator.GenerateLinePragmas = razorHost.EnableLinePragmas;
                    razorHost.Parser = new MvcCSharpRazorCodeParser();
                    break;

                case "vb":
                    razorHost.CodeGenerator = new VBRazorCodeGenerator(razorHost.DefaultClassName, razorHost.DefaultNamespace, path, razorHost);
                    razorHost.CodeGenerator.GenerateLinePragmas = razorHost.EnableLinePragmas;
                    razorHost.Parser = new MvcVBRazorCodeParser();
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Unknown language - " + razorHost.CodeLanguage.LanguageName);
            }
        }

        private void FixupDefaultClassNameIfTemplate(IRazorHost razorHost)
        {
            string filePath = Path.GetDirectoryName(razorHost.FullPath).TrimEnd(Path.DirectorySeparatorChar);
            if (filePath.EndsWith("EditorTemplates", StringComparison.OrdinalIgnoreCase) || 
                filePath.EndsWith("DisplayTemplates", StringComparison.OrdinalIgnoreCase))
            {
                // Fixes #133: For EditorTemplates \ DisplayTemplates, we'll suffix the file with an underscore to prevent name collisions
                razorHost.DefaultClassName = razorHost.DefaultClassName + '_';
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            if (generatedClass.BaseTypes.Count > 0)
            {
                CodeTypeReference codeTypeReference = (CodeTypeReference)generatedClass.BaseTypes[0];
                if (this._isSpecialPage)
                {
                    codeTypeReference.BaseType = typeof(ViewStartPage).FullName;
                }
                else if (!this._languageUtil.IsGenericTypeReference( codeTypeReference.BaseType))
                {
                    // Use the default model if it wasn't specified by the user.
                    codeTypeReference.BaseType = this._languageUtil.BuildGenericTypeReference(codeTypeReference.BaseType, new string[]{ this._languageUtil.DefaultModelTypeName});
                }
            }
        }

        private bool IsSpecialPage(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            return fileName.Equals(ViewStartFileName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
