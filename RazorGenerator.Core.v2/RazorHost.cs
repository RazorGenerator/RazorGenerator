﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.WebPages;
using System.IO;

namespace RazorGenerator.Core
{
    public class RazorHost : RazorEngineHost, IRazorHost, ICodeGenerationEventProvider
    {
        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net",
            "System.Text",
            "System.Web",
            "System.Web",
            "System.Web.Security",
            "System.Web.UI",
            "System.Web.WebPages",
            "System.Web.Helpers",
        };

        private readonly IRazorCodeTransformer _codeTransformer;
        private readonly string _baseRelativePath;
        private readonly string _fullPath;
        private readonly CodeDomProvider _codeDomProvider;
        private readonly IDictionary<string, string> _directives;
        private string _defaultClassName;
        private CodeLanguageUtil _languageUtil;

        public RazorHost(string baseRelativePath, string fullPath, IRazorCodeTransformer codeTransformer, CodeDomProvider codeDomProvider, IDictionary<string, string> directives)
            : base(RazorCodeLanguage.GetLanguageByExtension(Path.GetExtension(fullPath)))
        {

            if (codeTransformer == null)
            {
                throw new ArgumentNullException("codeTransformer");
            }
            if (baseRelativePath == null)
            {
                throw new ArgumentNullException("baseRelativePath");
            }
            if (fullPath == null)
            {
                throw new ArgumentNullException("fullPath");
            }
            if (codeDomProvider == null)
            {
                throw new ArgumentNullException("codeDomProvider");
            }
            _codeTransformer = codeTransformer;
            _baseRelativePath = baseRelativePath;
            _fullPath = fullPath;
            _codeDomProvider = codeDomProvider;
            _directives = directives;
            _languageUtil = Core.CodeLanguageUtil.GetLanguageUtilFromFileName(fullPath);
            base.DefaultNamespace = "ASP";
            EnableLinePragmas = true;

            base.GeneratedClassContext = new GeneratedClassContext(
                    executeMethodName: GeneratedClassContext.DefaultExecuteMethodName,
                    writeMethodName: GeneratedClassContext.DefaultWriteMethodName,
                    writeLiteralMethodName: GeneratedClassContext.DefaultWriteLiteralMethodName,
                    writeToMethodName: "WriteTo",
                    writeLiteralToMethodName: "WriteLiteralTo",
                    templateTypeName: typeof(HelperResult).FullName,
                    defineSectionMethodName: "DefineSection",
                    beginContextMethodName: "BeginContext",
                    endContextMethodName: "EndContext"
            )
            {
                ResolveUrlMethodName = "Href"
            };

            base.DefaultBaseClass = typeof(WebPage).FullName;
            foreach (var import in _defaultImports)
            {
                base.NamespaceImports.Add(import);
            }
        }

        public string ProjectRelativePath
        {
            get { return _baseRelativePath; }
        }

        public string FullPath
        {
            get { return _fullPath; }
        }

        public event EventHandler<GeneratorErrorEventArgs> Error;

        public event EventHandler<ProgressEventArgs> Progress;

        public override string DefaultClassName
        {
            get
            {
                return _defaultClassName ?? GetClassName();
            }
            set
            {
                if (!String.Equals(value, "__CompiledTemplate", StringComparison.OrdinalIgnoreCase))
                {
                    //  By default RazorEngineHost assigns the name __CompiledTemplate. We'll ignore this assignment
                    _defaultClassName = value;
                }
            }
        }

        public ParserBase Parser { get; set; }

        public RazorCodeGenerator CodeGenerator { get; set; }

        public bool EnableLinePragmas { get; set; }

        public CodeLanguageUtil CodeLanguageUtil
        {
            get
            {
                return _languageUtil;
            }
        }

        public string GenerateCode()
        {
            _codeTransformer.Initialize(this, _directives);

            // Create the engine
            RazorTemplateEngine engine = new RazorTemplateEngine(this);

            // Generate code 
            GeneratorResults results = null;
            try
            {
                Stream stream = File.OpenRead(_fullPath);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    results = engine.GenerateCode(reader, className: DefaultClassName, rootNamespace: DefaultNamespace, sourceFileName: _fullPath);
                }
            }
            catch (Exception e)
            {
                OnGenerateError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }

            // Output errors
            foreach (RazorError error in results.ParserErrors)
            {
                OnGenerateError(4, error.Message, (uint)error.Location.LineIndex + 1, (uint)error.Location.CharacterIndex + 1);
            }

            try
            {
                OnCodeCompletion(50, 100);

                using (StringWriter writer = new StringWriter())
                {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    //Generate the code
                    writer.WriteLine(CodeLanguageUtil.GetPreGeneratedCodeBlock());
                    _codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, writer, options);
                    writer.WriteLine(CodeLanguageUtil.GetPostGeneratedCodeBlock());

                    OnCodeCompletion(100, 100);
                    writer.Flush();

                    // Perform output transformations and return
                    string codeContent = writer.ToString();
                    codeContent = _codeTransformer.ProcessOutput(codeContent);
                    return codeContent;
                }
            }
            catch (Exception e)
            {
                OnGenerateError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }

        public override void PostProcessGeneratedCode(CodeGeneratorContext context)
        {
            _codeTransformer.ProcessGeneratedCode(context.CompileUnit, context.Namespace, context.GeneratedClass, context.TargetMethod);
        }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            var codeGenerator = CodeGenerator ?? base.DecorateCodeGenerator(incomingCodeGenerator);
            codeGenerator.GenerateLinePragmas = EnableLinePragmas;
            return codeGenerator;
        }

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return Parser ?? base.DecorateCodeParser(incomingCodeParser);
        }

        private void OnGenerateError(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber)
        {
            if (Error != null)
            {
                Error(this, new GeneratorErrorEventArgs(errorCode, errorMessage, lineNumber, columnNumber));
            }
        }

        private void OnCodeCompletion(uint completed, uint total)
        {
            if (Progress != null)
            {
                Progress(this, new ProgressEventArgs(completed, total));
            }
        }

        protected virtual string GetClassName()
        {
            return ParserHelpers.SanitizeClassName(_baseRelativePath);
        }
    }
}
