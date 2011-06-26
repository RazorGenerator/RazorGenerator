using System;
using System.Linq;

namespace TemplateSample {
    class Program {
        static void Main(string[] args)
        {
            // Create the template
            var preprocessedTemplate = new PreProcessedTemplate();

            // Give it some input data
            preprocessedTemplate.TestResults =
                from i in Enumerable.Range(0, 40)
                select new TestResult {
                    Id = Guid.NewGuid(),
                    Name = "Test" + i,
                    Passed = i % 2 == 0
                };

            // Run it
            Console.WriteLine(preprocessedTemplate.TransformText());
        }
    }
}
