using System;
using System.Globalization;
using System.Text;

namespace RazorGenerator.Templating
{
    public class RazorTemplateBase
    {
        private string _content;
        public RazorTemplateBase Layout { get; set; }

        private StringBuilder _generatingEnvironment = new System.Text.StringBuilder();

        public virtual void Execute()
        {
        }

        public void WriteLiteral(string textToAppend)
        {
            if (String.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            _generatingEnvironment.Append(textToAppend); ;
        }

        public void Write(object value)
        {
            if ((value == null))
            {
                return;
            }

            WriteLiteral(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public string RenderBody()
        {
            return _content;
        }

        public string TransformText()
        {
            Execute();
            if (Layout != null)
            {
                Layout._content = _generatingEnvironment.ToString();
                return Layout.TransformText();
            }
            else
            {
                return _generatingEnvironment.ToString();
            }
        }
    }
}
