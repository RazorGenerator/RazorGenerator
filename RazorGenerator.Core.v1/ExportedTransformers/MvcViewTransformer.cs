using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Razor;
using System.Web.Razor;

namespace RazorGenerator.Core
{
    [Export("MvcView", typeof(IRazorCodeTransformer))]
    public class MvcViewTransformer : AggregateCodeTransformer
    {
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

            razorHost.CodeGenerator = new MvcCodeGenerator(razorHost.DefaultClassName, razorHost.DefaultBaseClass, razorHost.DefaultNamespace, razorHost.FullPath, razorHost);
            razorHost.CodeGenerator.GenerateLinePragmas = razorHost.EnableLinePragmas;
            razorHost.Parser = new MvcCSharpRazorCodeParser();
        }

        /// <summary>
        /// MvcCSharpRazorCodeGenerator has a strong dependency on the MvcHost which is something I don't want to deal with.
        /// This code is essentially copied from the MvcHost
        /// </summary>
        private sealed class MvcCodeGenerator : MvcCSharpRazorCodeGenerator
        {
            private const string DefaultModelTypeName = "dynamic";
            private const string ViewStartFileName = "_ViewStart";

            public MvcCodeGenerator(string className, string baseClass, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
                : base(className, rootNamespaceName, sourceFileName, host)
            {
                string baseType;

                if (IsSpecialPage(sourceFileName))
                {
                    baseType = typeof(ViewStartPage).FullName;
                }
                else
                {
                    baseType = baseClass + '<' + DefaultModelTypeName + '>';
                }
                SetBaseType(baseType);
            }

            private bool IsSpecialPage(string path)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return fileName.Equals(ViewStartFileName, StringComparison.OrdinalIgnoreCase);
            }

            private void SetBaseType(string name)
            {
                var baseType = new CodeTypeReference(name);
                GeneratedClass.BaseTypes.Clear();
                GeneratedClass.BaseTypes.Add(baseType);
            }
        }
    }
}
