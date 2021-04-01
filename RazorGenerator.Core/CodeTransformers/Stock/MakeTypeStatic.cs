namespace RazorGenerator.Core.CodeTransformers
{
    public class MakeTypeStatic : RazorCodeTransformerBase
    {
        public override string ProcessOutput(string codeContent)
        {
            return this.razorHost.CodeLanguageUtil.MakeTypeStatic(codeContent);
        }
    }
}
