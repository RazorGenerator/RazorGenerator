using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleRazorHelperLibrary {
    public partial class PreProcessedTemplate {
        public IEnumerable<TestResult> TestResults {
            get {
                return from i in Enumerable.Range(0, 40)
                       select new TestResult { Id = Guid.NewGuid(), Name = "Test" + i, Passed = i % 2 == 0 };
            }
        }
    }

    public class TestResult {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool Passed { get; set; }
    }
}
