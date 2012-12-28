using System.IO;

namespace System.Web.WebPages
{
    /// <summary>
    /// This is just to avoid adding System.Web.WebPages to the sample application
    /// </summary>
    public class HelperResult
    {
        Action<TextWriter> _callback;

        public HelperResult(Action<TextWriter> callback)
        {
            _callback = callback;
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                _callback(writer);
                return writer.ToString();
            }
        }
    }
}
