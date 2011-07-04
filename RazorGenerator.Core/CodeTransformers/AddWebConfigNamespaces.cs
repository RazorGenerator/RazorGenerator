using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Configuration;
using System.Web.WebPages.Razor.Configuration;

namespace RazorGenerator.Core {
    public class AddWebConfigNamespaces : RazorCodeTransformerBase {
        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives) {
            string projectPath = GetProjectRoot(razorHost.ProjectRelativePath, razorHost.FullPath);
            string currentPath = razorHost.FullPath;
            string directoryVirtualPath = null;

            var configFileMap = new WebConfigurationFileMap();
                
            var virtualDirectories = configFileMap.VirtualDirectories;
            while (!currentPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase)) {
                currentPath = Path.GetDirectoryName(currentPath);
                string relativePath = currentPath.Substring(projectPath.Length);
                bool isAppRoot = currentPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase);
                string virtualPath = relativePath.Replace('\\', '/');
                if (virtualPath.Length == 0) {
                    virtualPath = "/";
                }

                directoryVirtualPath = directoryVirtualPath ?? virtualPath;

                virtualDirectories.Add(virtualPath, new VirtualDirectoryMapping(currentPath, isAppRoot: isAppRoot));
            }

            var config = WebConfigurationManager.OpenMappedWebConfiguration(configFileMap, directoryVirtualPath);
            var section = (RazorPagesSection)config.GetSection(RazorPagesSection.SectionName);

            if (section != null) {
                foreach (NamespaceInfo n in section.Namespaces) {
                    razorHost.NamespaceImports.Add(n.Namespace);
                }
            }
        }

        private static string GetProjectRoot(string projectRelativePath, string fullPath) {
            int index = fullPath.LastIndexOf(projectRelativePath);
            if (index != -1) {
                return fullPath.Substring(0, index);
            }
            else {
                return Path.GetDirectoryName(fullPath);
            }
        }
    }
}
