using System;
using System.Linq;

namespace TemplateSample
{
    class Program
    {
        static void Main(string[] args)
        {
            RenderTestTemplate();
            RenderMailTemplate();
            RenderListTemplate();
            RenderHelperTestTemplate();
            Console.ReadKey();
        }

        private static void RenderTestTemplate()
        {
            // Create the template
            var preprocessedTemplate = new PreProcessedTemplate();

            // Give it some input data
            preprocessedTemplate.TestResults =
                from i in Enumerable.Range(0, 40)
                select new TestResult
                {
                    Id = Guid.NewGuid(),
                    Name = "Test" + i,
                    Passed = i % 2 == 0
                };

            // Run it
            Console.WriteLine(preprocessedTemplate.TransformText());
        }

        private static void RenderMailTemplate()
        {
            // Create the template
            var preprocessedTemplate = new MailTemplate()
            {
                Layout = new MyLayout()
            };

            // Give it some input data

            // Run it
            Console.WriteLine(preprocessedTemplate.TransformText());
        }

        private static void RenderListTemplate()
        {
            var listTemplate = new ListTemplate
            {
                Foos = new[] { "one", "two", "three" }
            };

            Console.WriteLine(listTemplate.TransformText());
        }

        private static void RenderHelperTestTemplate()
        {
            var helperTestTemplate = new HelperTestTemplate();
            Console.WriteLine(helperTestTemplate.TransformText());
        }
    }
}
