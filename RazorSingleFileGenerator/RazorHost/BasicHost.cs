using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.WebPages;
using System.Web.WebPages.Razor;
using System;

namespace RazorGenerator.RazorHost {
    [Export("Basic", typeof(ISingleFileGenerator))]
    public class BasicHost : RazorEngineHost, ISingleFileGenerator {
        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net",
            "System.Web",
            "System.Web",
            "System.Web.Security",
        };
        private string _defaultClassName;


        public BasicHost()
            : base(RazorCodeLanguage.GetLanguageByExtension(".cshtml")) {

            GeneratedClassContext = new GeneratedClassContext(GeneratedClassContext.DefaultExecuteMethodName, GeneratedClassContext.DefaultWriteMethodName,
                GeneratedClassContext.DefaultWriteLiteralMethodName, "WriteTo", "WriteLiteralTo", typeof(HelperResult).FullName, "DefineSection");
            DefaultBaseClass = typeof(WebPage).FullName;
            foreach (var import in _defaultImports) {
                NamespaceImports.Add(import);
            }
        }

        [Import("fileNamespace")]
        public string FileNamespace { get; set; }

        [Import("projectRelativePath")]
        public string ProjectRelativePath { get; set; }

        [Import("fullPath")]
        public string FullPath { get; set; }

        public override string DefaultNamespace {
            get {
                return FileNamespace ?? "ASP";
            }
            set {
                FileNamespace = value;
            }
        }

        public virtual IEnumerable<string> DefaultImports {
            get {
                return _defaultImports;
            }
        }

        public override string DefaultClassName {
            get {
                _defaultClassName = _defaultClassName ?? GetClassName(GetVirtualPath(ProjectRelativePath));
                return _defaultClassName;
            }
            set {
                if (!String.Equals(value, "__CompiledTemplate", StringComparison.OrdinalIgnoreCase)) {
                    //  By default RazorEngineHost assigns the name __CompiledTemplate. We'll ignore this assignment
                    _defaultClassName = value;
                }
            }
        }

        public virtual void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives) {
            // Do nothing    
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod) {
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            generatedNamespace.Imports.AddRange((from s in _defaultImports
                                                 select new CodeNamespaceImport(s)).ToArray());
        }

        protected virtual string GetClassName(string virtualPath) {
            string filename = Path.GetFileNameWithoutExtension(virtualPath);
            return ParserHelpers.SanitizeClassName(filename);
        }

        protected static string GetVirtualPath(string projectRelativePath) {
            return VirtualPathUtility.ToAppRelative("~" + projectRelativePath);
        }
    }
}
