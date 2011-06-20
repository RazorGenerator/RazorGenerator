using System;
using System.Linq;

namespace ConsoleApplicationPreProcessedRazorTemplate {
    class Program {
        static void Main(string[] args)
        {
            var preprocessedTemplate = new PreProcessedTemplate {
                TestResults =
                    from i in Enumerable.Range(0, 40)
                    select new TestResult {
                        Id = Guid.NewGuid(),
                        Name = "Test" + i,
                        Passed = i%2 == 0
                    }
            };

            Console.WriteLine(preprocessedTemplate.TransformText());
        }
    }
}
