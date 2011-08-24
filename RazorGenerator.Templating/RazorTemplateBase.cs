using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorGenerator.Templating {
    public class RazorTemplateBase {
        public RazorTemplateBase Layout { get; set; }

        private StringBuilder _generatingEnvironment = new System.Text.StringBuilder();

        public virtual void Execute() {
        }

        public void WriteLiteral(string textToAppend) {
            if (String.IsNullOrEmpty(textToAppend)) {
                return;
            }
            _generatingEnvironment.Append(textToAppend); ;
        }

        public void Write(object value) {

            if ((value == null)) {
                return;
            }
            string stringValue;
            System.Type t = value.GetType();
            System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
            if ((method == null)) {
                stringValue = value.ToString();
            }
            else {
                stringValue = ((string)(method.Invoke(value, new object[] { System.Globalization.CultureInfo.InvariantCulture })));
            }
            WriteLiteral(stringValue);

        }

        string _content;

        public string RenderBody() {
            return _content;
        }

        public string TransformText() {
            Execute();
            if (Layout != null) {
                Layout._content = _generatingEnvironment.ToString();
                return Layout.TransformText();
            }
            else {
                return _generatingEnvironment.ToString();
            }
        }
    }
}
