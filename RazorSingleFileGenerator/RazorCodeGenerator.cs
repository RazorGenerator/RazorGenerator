using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace Microsoft.Web.RazorSingleFileGenerator {
    public class RazorCodeGenerator {
        public delegate void RazorErrorEventHandler(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber);

        public delegate int CodeCompletionEventHandler(uint completed, uint total);

        public RazorErrorEventHandler ErrorHandler { get; set; }

        public CodeCompletionEventHandler CompletionHandler { get; set; }

        public bool GenerateStaticType { get; set; }

        private void OnGenerateError(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber) {
            if (ErrorHandler != null) {
                ErrorHandler(errorCode, errorMessage, lineNumber, columnNumber);
            }
        }

        private void OnCodeCompletion(uint completed, uint total) {
            if (CompletionHandler != null) {
                CompletionHandler(completed, total);
            }
        }

        public byte[] GenerateCode(string inputFileContent, RazorEngineHost razorHost, CodeDomProvider codeDomProvider) {

            // Create the host and engine
            RazorTemplateEngine engine = new RazorTemplateEngine(razorHost);

            // Generate code
            GeneratorResults results = null;
            try {
                using (TextReader reader = new StringReader(inputFileContent)) {
                    results = engine.GenerateCode(reader);
                }
            }
            catch (Exception e) {
                OnGenerateError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }

            // Output errors
            foreach (RazorError error in results.ParserErrors) {
                OnGenerateError(4, error.Message, (uint)error.Location.LineIndex + 1, (uint)error.Location.CharacterIndex + 1);
            }

            try {
                OnCodeCompletion(50, 100);

                using (StringWriter writer = new StringWriter(new StringBuilder())) {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    //Generate the code
                    writer.WriteLine("#pragma warning disable 1591");
                    codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, writer, options);
                    writer.WriteLine("#pragma warning restore 1591");

                    OnCodeCompletion(100, 100);
                    writer.Flush();

                    //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
                    //which may not work with all languages
                    Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

                    //Get the preamble (byte-order mark) for our encoding
                    byte[] preamble = enc.GetPreamble();
                    int preambleLength = preamble.Length;

                    //Convert the writer contents to a byte array
                    string codeContent = writer.ToString();
                    if (GenerateStaticType) {
                        codeContent = codeContent.Replace("public class", "public static class");
                    }
                    byte[] body = enc.GetBytes(codeContent);

                    //Prepend the preamble to body (store result in resized preamble array)
                    Array.Resize<byte>(ref preamble, preambleLength + body.Length);
                    Array.Copy(body, 0, preamble, preambleLength, body.Length);

                    //Return the combined byte array
                    return preamble;
                }
            }
            catch (Exception e) {
                OnGenerateError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }
    }
}
