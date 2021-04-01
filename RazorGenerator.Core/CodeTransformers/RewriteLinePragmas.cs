namespace RazorGenerator.Core.CodeTransformers
{
    public sealed class RewriteLinePragmas : RazorCodeTransformerBase
    {
        private string _binRelativePath;
        private string _fullPath;

        public override void Initialize(IRazorHost razorHost, System.Collections.Generic.IDictionary<string, string> directives)
        {
            this._binRelativePath = @"..\.." + razorHost.ProjectRelativePath;
            this._fullPath = razorHost.FullPath;
        }

        public override string ProcessOutput(string codeContent)
        {
            return codeContent.Replace("\"" + this._fullPath + "\"", "\"" + this._binRelativePath + "\"");
        }
    }
}
