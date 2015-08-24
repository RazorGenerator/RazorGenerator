using System;
using System.Globalization;
using System.Text;
using System.IO;

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

        public void Write(bool value)    { WriteLiteral(value.ToString()); }
        public void Write(int value)     { WriteLiteral(value.ToString(CultureInfo.InvariantCulture)); }
        public void Write(long value)    { WriteLiteral(value.ToString(CultureInfo.InvariantCulture)); }
        public void Write(float value)   { WriteLiteral(value.ToString(CultureInfo.InvariantCulture)); }
        public void Write(double value)  { WriteLiteral(value.ToString(CultureInfo.InvariantCulture)); }
        public void Write(decimal value) { WriteLiteral(value.ToString(CultureInfo.InvariantCulture)); }

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

        public void Clear()
        {
            _generatingEnvironment.Clear();

            if (Layout != null)
            {
                Layout._content = "";
            }
        }

        public void WriteTo(TextWriter writer, object value)
        {
            writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public void WriteTo(TextWriter writer, bool value)     { writer.Write(value.ToString()); }
        public void WriteTo(TextWriter writer, int value)      { writer.Write(value.ToString(CultureInfo.InvariantCulture)); }
        public void WriteTo(TextWriter writer, long value)     { writer.Write(value.ToString(CultureInfo.InvariantCulture)); }
        public void WriteTo(TextWriter writer, float value)    { writer.Write(value.ToString(CultureInfo.InvariantCulture)); }
        public void WriteTo(TextWriter writer, double value)   { writer.Write(value.ToString(CultureInfo.InvariantCulture)); }
        public void WriteTo(TextWriter writer, decimal value)  { writer.Write(value.ToString(CultureInfo.InvariantCulture)); }
    }
}
