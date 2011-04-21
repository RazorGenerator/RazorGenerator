using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;

namespace Microsoft.Web.RazorSingleFileGenerator
{
    public class RazorHostLocator
    {
        public const string ConfigurationFilename = "RazorGenerators.config";

        private readonly Project _project;
        private readonly Solution _solution;

        public RazorHostLocator(Project project)
        {
            if(project == null)
                throw new ArgumentNullException("project");

            _project = project;
            _solution = project.DTE.Solution;
        }

        public IDictionary<string, Type> GetRazorHostTypesByName()
        {
            var customHostAssemblies = GetCustomHostAssemblies();

            var singleFileGeneratorTypes = 
                customHostAssemblies
                    .Union(new [] { typeof(ISingleFileGenerator).Assembly })
                    .SelectMany(x => x.GetExportedTypes())
                    .Where(type => !type.IsAbstract && !type.IsInterface)
                    .Where(typeof(ISingleFileGenerator).IsAssignableFrom);

            return 
                singleFileGeneratorTypes
                    .OrderBy(type => type.Name)
                    .ToDictionary(type => type.Name.Replace("Host", null), StringComparer.Ordinal);
        }

        private IEnumerable<Assembly> GetCustomHostAssemblies()
        {
            IEnumerable<string> customAssemblyFilenames =
                GetCustomHostAssemblyNamesFrom(_project.FullName)
                    .Union(GetCustomHostAssemblyNamesFrom(_solution.FullName))
                    .Distinct();

            IEnumerable<Assembly> customAssemblies =
                from assemblyName in customAssemblyFilenames
                where !string.IsNullOrWhiteSpace(assemblyName)
                select Assembly.LoadFrom(assemblyName.Trim());  // Let load exceptions bubble up to the UI

            return customAssemblies;
        }

        private IEnumerable<string> GetCustomHostAssemblyNamesFrom(string projectItemFilename)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(projectItemFilename), "Project Item Filename is empty");

            var directory = Path.GetDirectoryName(projectItemFilename);
            var configFile = Path.Combine(directory, ConfigurationFilename);

            if (!File.Exists(configFile))
                return Enumerable.Empty<string>();

            var customAssemblyNames =
                from line in File.ReadAllLines(configFile)
                where !string.IsNullOrWhiteSpace(line)
                select ResolveAssemblyPath(line, directory);

            return customAssemblyNames;
        }

        private string ResolveAssemblyPath(string line, string directory)
        {
            // If it's an absolute path, we're fine
            if(Path.IsPathRooted(line))
                return line;

            // Otherwise it's relative to the directory, 
            // so absolute-ize it
            return Path.Combine(directory, line);
        }
    }
}
