namespace RazorGenerator.Core
{
    internal sealed class RewriteLinePragmas : RazorCodeTransformerBase
    {
        private string _binRelativePath;
        private string _fullPath;

        public override void Initialize(RazorHost razorHost, System.Collections.Generic.IDictionary<string, string> directives)
        {
            _binRelativePath = @"..\.." + razorHost.ProjectRelativePath;
            _fullPath = razorHost.FullPath;
        }

        public override string ProcessOutput(string codeContent)
        {
            return codeContent.Replace("\"" + _fullPath + "\"", "\"" + _binRelativePath + "\"");
        }
    }
}
