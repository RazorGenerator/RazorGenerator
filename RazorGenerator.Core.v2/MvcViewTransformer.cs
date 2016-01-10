using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Razor;
using System.Web.Razor.Generator;

namespace RazorGenerator.Core
{
    [Export("MvcView", typeof(IRazorCodeTransformer))]
    public class MvcViewTransformer : AggregateCodeTransformer
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
        private CodeLanguageUtil _languageutil;

        internal static IEnumerable<string> MvcNamespaces
        {
            get { return _namespaces; }
        }

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _codeTransformers; }
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            base.Initialize(razorHost, directives);
            _languageutil = razorHost.CodeLanguageUtil;

            _isSpecialPage = IsSpecialPage(razorHost.FullPath);
            FixupDefaultClassNameIfTemplate(razorHost);


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

        private void FixupDefaultClassNameIfTemplate(RazorHost razorHost)
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
                var codeTypeReference = (CodeTypeReference)generatedClass.BaseTypes[0];
                if (_isSpecialPage)
                {
                    codeTypeReference.BaseType = typeof(ViewStartPage).FullName;
                }
                else if (!_languageutil.IsGenericTypeReference( codeTypeReference.BaseType))
                {
                    // Use the default model if it wasn't specified by the user.
                    codeTypeReference.BaseType = _languageutil.BuildGenericTypeReference(codeTypeReference.BaseType, new string[]{ _languageutil.DefaultModelTypeName});
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
