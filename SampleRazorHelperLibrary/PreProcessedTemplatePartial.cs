
namespace SampleRazorHelperLibrary {
    /// <remarks>
    /// Usage Example:
    /// var test = new Test("MyScope", false, true, true, "Foo", "_foo", "string", "OnChange");
    /// string result = test.TransformText();
    /// </remarks>
    public partial class PreProcessedTemplate {
        public PreProcessedTemplate(string scope, bool isShared, bool isReadOnly, bool isSetterPrivate,
                    string propertyName, string backingField, string typeName,
                    string raisePropertyChangedMethodName) {

            Scope = scope;
            Shared = isShared;
            ReadOnly = isReadOnly;
            IsSetterPrivate = isSetterPrivate;
            PropertyName = propertyName;
            BackingField = backingField;
            TypeName = typeName;
            RaisePropertyChangedMethodName = raisePropertyChangedMethodName;
        }

        public string PropertyName { get; set; }

        public string BackingField { get; set; }

        public string TypeName { get; set; }

        public bool Shared { get; set; }

        public string RaisePropertyChangedMethodName { get; set; }

        public bool ReadOnly { get; set; }

        public string Scope { get; set; }

        public bool IsSetterPrivate { get; set; }
    }

        
}
