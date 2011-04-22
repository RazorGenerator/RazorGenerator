using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;

namespace Microsoft.Web.RazorSingleFileGenerator.RazorHost {
    [Export("Template", typeof(ISingleFileGenerator))]
    public class TemplateHost : RazorEngineHost, ISingleFileGenerator {
        private static readonly IEnumerable<string> _defaultNamespaces = new[] {
            typeof(System.String).Namespace,
            typeof(System.Collections.Generic.ICollection<>).Namespace,
            typeof(System.Linq.Enumerable).Namespace,
            typeof(System.Text.StringBuilder).Namespace,
        };

        private const string GenerationEnvironmentPropertyName = "GenerationEnvironment";
        private readonly string _projectRelativePath;

        // Need to have these parameters here because the RazorGenerator looks up. Need to refactor them so it isn't so ugly.
        [ImportingConstructor]
        public TemplateHost([Import("fileNamespace")] string fileNamespace,
                            [Import("projectRelativePath")] string projectRelativePath) {
            DefaultNamespace = fileNamespace;
            GeneratedClassContext = new GeneratedClassContext(GeneratedClassContext.DefaultExecuteMethodName,
                                                              GeneratedClassContext.DefaultWriteMethodName,
                                                              GeneratedClassContext.DefaultWriteLiteralMethodName);
            _projectRelativePath = projectRelativePath;
            DefaultClassName = GetClassName(projectRelativePath);
            DefaultBaseClass = DefaultClassName + "Base";
            CodeLanguage = RazorCodeLanguage.GetLanguageByExtension(Path.GetExtension(_projectRelativePath));
            foreach (var item in _defaultNamespaces) {
                NamespaceImports.Add(item);
            }
        }

        public override string DefaultClassName { get; set; }

        public override RazorCodeLanguage CodeLanguage { get; protected set; }

        protected virtual string GetClassName(string projectRelativePath) {
            return Path.GetFileNameWithoutExtension(projectRelativePath);
        }

        public override MarkupParser CreateMarkupParser() {
            return new HtmlMarkupParser();
        }

        public void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives) {

        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod) {
            generatedClass.IsPartial = true;
            // The generated class has a constructor in there by default.
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());

            ProvideTransformTextMethod(generatedClass);
            generatedNamespace.Types.Add(ProvideBaseType());
        }

        private static void ProvideTransformTextMethod(CodeTypeDeclaration generatedClass) {
            var method = new CodeMemberMethod {
                Name = "TransformText",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(typeof(string))
            };

            method.Statements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), GeneratedClassContext.DefaultExecuteMethodName));

            method.Statements.Add(new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), GenerationEnvironmentPropertyName),
                        "ToString")
            )));
            generatedClass.Members.Add(method);
        }

        private CodeTypeDeclaration ProvideBaseType() {
            var baseType = new CodeTypeDeclaration(DefaultBaseClass) {
                IsPartial = false,
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            baseType.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Base class"));
            baseType.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "Base class"));

            ProvideExecuteMethod(baseType);
            ProvideGenerationEnvironmentProperty(baseType);
            ProvideCurrentIndentProperty(baseType);
            ProvideEndsWithNewLineField(baseType);
            ProvideWriteLiteralMethod(baseType);
            ProvideWriteMethod(baseType);
            return baseType;
        }

        private static void ProvideExecuteMethod(CodeTypeDeclaration baseType) {
            var method = new CodeMemberMethod {
                Name = "Execute",
                Attributes = MemberAttributes.Public
            };

            baseType.Members.Add(method);
        }

        private static void ProvideGenerationEnvironmentProperty(CodeTypeDeclaration baseType) {
            var builderType = new CodeTypeReference(typeof(StringBuilder));
            var backingField = new CodeMemberField {
                Type = builderType,
                Name = "_generatingEnvironment",
                Attributes = MemberAttributes.Private,
                InitExpression = new CodeObjectCreateExpression(builderType)
            };

            baseType.Members.Add(backingField);

            var property = new CodeMemberProperty {
                Name = GenerationEnvironmentPropertyName,
                Type = builderType,
                Attributes = MemberAttributes.Family | MemberAttributes.Final,
            };

            var fieldReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_generatingEnvironment");
            property.GetStatements.Add(new CodeMethodReturnStatement(fieldReference));
            property.SetStatements.Add(new CodeAssignStatement(fieldReference, new CodePropertySetValueReferenceExpression()));

            baseType.Members.Add(property);
        }

        private static void ProvideCurrentIndentProperty(CodeTypeDeclaration baseType) {
            var builderType = new CodeTypeReference(typeof(StringBuilder));
            var backingField = new CodeMemberField {
                Type = builderType,
                Name = "_currentIndent",
                Attributes = MemberAttributes.Private,
                InitExpression = new CodeObjectCreateExpression(builderType)
            };

            baseType.Members.Add(backingField);

            var property = new CodeMemberProperty {
                Name = "CurrentIndent",
                Type = builderType,
                Attributes = MemberAttributes.Public,
            };

            var fieldReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_currentIndent");
            property.GetStatements.Add(new CodeMethodReturnStatement(fieldReference));
            property.SetStatements.Add(new CodeAssignStatement(fieldReference, new CodePropertySetValueReferenceExpression()));

            baseType.Members.Add(property);
        }

        private static void ProvideEndsWithNewLineField(CodeTypeDeclaration baseType) {
            var field = new CodeMemberField {
                Type = new CodeTypeReference(typeof(bool)),
                Name = "_endsWithNewline",
                Attributes = MemberAttributes.Private
            };

            baseType.Members.Add(field);
        }

        // The body of the following two methods were stolen from T4 templates
        private static void ProvideWriteMethod(CodeTypeDeclaration baseType) {
            var method = new CodeMemberMethod {
                Name = "Write",
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "value"));

            method.Statements.Add(new CodeSnippetStatement(@"
                string stringValue;
                if ((value == null))
                {
                    throw new global::System.ArgumentNullException(""value"");
                }
                System.Type t = value.GetType();
                System.Reflection.MethodInfo method = t.GetMethod(""ToString"", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null)) 
                {
                    stringValue = value.ToString();
                }
                else {
                    stringValue = ((string)(method.Invoke(value, new object[] { System.Globalization.CultureInfo.InvariantCulture })));
                }
                WriteLiteral(stringValue);
            "));
            baseType.Members.Add(method);
        }

        private static void ProvideWriteLiteralMethod(CodeTypeDeclaration baseType) {
            var method = new CodeMemberMethod {
                Name = "WriteLiteral",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
            };
            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "textToAppend"));


            method.Statements.Add(new CodeSnippetExpression(@"
        if (string.IsNullOrEmpty(textToAppend)) {
            return;
        }
        // If we're starting off, or if the previous text ended with a newline,
        // we have to append the current indent first.
        if (((this.GenerationEnvironment.Length == 0)
                    || this._endsWithNewline)) {
            this.GenerationEnvironment.Append(this._currentIndent);
            this._endsWithNewline = false;
        }
        // Check if the current text ends with a newline
        if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture)) {
            this._endsWithNewline = true;
        }
        // This is an optimization. If the current indent is "", then we don't have to do any
        // of the more complex stuff further down.
        if ((this._currentIndent.Length == 0)) {
            this.GenerationEnvironment.Append(textToAppend);
            return;
        }
        // Everywhere there is a newline in the text, add an indent after it
        textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this._currentIndent));
        // If the text ends with a newline, then we should strip off the indent added at the very end
        // because the appropriate indent will be added when the next time Write() is called
        if (this._endsWithNewline) {
            this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this._currentIndent.Length));
        }
        else {
            this.GenerationEnvironment.Append(textToAppend);
        }

            "));

            baseType.Members.Add(method);
        }
    }
}
