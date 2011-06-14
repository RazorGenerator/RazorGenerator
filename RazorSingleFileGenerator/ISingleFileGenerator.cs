using System.Collections.Generic;

namespace RazorGenerator {
    public interface ISingleFileGenerator {
        void PreCodeGeneration(RazorCodeGenerator codeGenerator, IDictionary<string, string> directives);
    }
}
