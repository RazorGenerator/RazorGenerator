using System;
using System.Collections.Generic;

namespace ConsoleApplicationPreProcessedRazorTemplate {
    public partial class PreProcessedTemplate {
        public IEnumerable<TestResult> TestResults { get; set; }
    }

    public class TestResult {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool Passed { get; set; }
    }
}
