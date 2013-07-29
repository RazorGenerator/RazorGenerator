using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Configuration;
using System.Web.WebPages.Razor.Configuration;
using System.Linq;

namespace RazorGenerator.Core
{
    public class MvcWebConfigTransformer : AggregateCodeTransformer
    {
        private const string DefaultBaseType = "System.Web.Mvc.WebViewPage";
        private readonly List<RazorCodeTransformerBase> _transformers = new List<RazorCodeTransformerBase>();

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _transformers; ; }
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            string projectPath = GetProjectRoot(razorHost.ProjectRelativePath, razorHost.FullPath).TrimEnd(Path.DirectorySeparatorChar);
            string currentPath = razorHost.FullPath;
            string directoryVirtualPath = null;

            var configFileMap = new WebConfigurationFileMap();

            var virtualDirectories = configFileMap.VirtualDirectories;
            while (!currentPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                currentPath = Path.GetDirectoryName(currentPath);
                string relativePath = currentPath.Substring(projectPath.Length);
                bool isAppRoot = currentPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase);
                string virtualPath = relativePath.Replace('\\', '/');
                if (virtualPath.Length == 0)
                {
                    virtualPath = "/";
                }

                directoryVirtualPath = directoryVirtualPath ?? virtualPath;

                virtualDirectories.Add(virtualPath, new VirtualDirectoryMapping(currentPath, isAppRoot: isAppRoot));
            }

            try
            {
                var config = WebConfigurationManager.OpenMappedWebConfiguration(configFileMap, directoryVirtualPath);
                RazorPagesSection section = config.GetSection(RazorPagesSection.SectionName) as RazorPagesSection;
                if (section != null)
                {
                    string baseType = section.PageBaseType;
                    if (!DefaultBaseType.Equals(baseType, StringComparison.OrdinalIgnoreCase))
                    {
                        _transformers.Add(new SetBaseType(baseType));
                    }

                    if (section != null)
                    {
                        foreach (NamespaceInfo n in section.Namespaces)
                        {
                            razorHost.NamespaceImports.Add(n.Namespace);
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Bug in Mono framework.
                // Configure namespaces using the RazorGenerator directives file instead.
            }
            base.Initialize(razorHost, directives);
        }

        private static string GetProjectRoot(string projectRelativePath, string fullPath)
        {
            int index = fullPath.LastIndexOf(projectRelativePath);
            if (index != -1)
            {
                return fullPath.Substring(0, index);
            }
            else
            {
                return Path.GetDirectoryName(fullPath);
            }
        }
    }
}
