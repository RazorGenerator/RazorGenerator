using System.Collections.Generic;

namespace Microsoft.Web.RazorSingleFileGenerator {
    public interface ISingleFileGenerator {
        void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives);
    }
}
