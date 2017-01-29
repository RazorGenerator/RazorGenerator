using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RazorGenerator.Core;
using System.Xml.Linq;

namespace RazorGenerator.MsBuild
{
    public class RazorCodeGen : Task
    {
        private static readonly Regex _namespaceRegex = new Regex(@"($|\.)(\d)");
        private readonly List<ITaskItem> _generatedFiles = new List<ITaskItem>();

        public ITaskItem[] FilesToPrecompile { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        [Required]
        public string ProjectName { get; set; }

        public string RootNamespace { get; set; }

        [Required]
        public bool IncludeNewViewsInProject { get; set; }

        [Required]
        public string CodeGenDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles
        {
            get
            {
                return _generatedFiles.ToArray();
            }
        }

        public override bool Execute()
        {
            try
            {
                return ExecuteCore();
            }
            catch (Exception ex)
            {
                Log.LogError(ex.Message);
            }
            return false;
        }

        private bool ExecuteCore()
        {
            if (FilesToPrecompile == null || !FilesToPrecompile.Any())
            {
                return true;
            }

            string projectRoot = String.IsNullOrEmpty(ProjectRoot) ? Directory.GetCurrentDirectory() : ProjectRoot;
            using (var hostManager = new HostManager(projectRoot))
            {
                foreach (var file in FilesToPrecompile)
                {
                    string filePath = file.GetMetadata("FullPath");
                    string fileName = Path.GetFileName(filePath);
                    var projectRelativePath = GetProjectRelativePath(filePath, projectRoot);
                    string itemNamespace = GetNamespace(file, projectRelativePath);

                    CodeLanguageUtil langutil = CodeLanguageUtil.GetLanguageUtilFromFileName(fileName);

                    var precompiledViewRelativePath = projectRelativePath.TrimStart(Path.DirectorySeparatorChar) + langutil.GetCodeFileExtension();
                    string outputPath = Path.Combine(CodeGenDirectory, precompiledViewRelativePath);
                    if (!RequiresRecompilation(filePath, outputPath))
                    {
                        Log.LogMessage(MessageImportance.Low, "Skipping file {0} since {1} is already up to date", filePath, outputPath);
                        continue;
                    }
                    EnsureDirectory(outputPath);

                    Log.LogMessage(MessageImportance.Low, "Precompiling {0} at path {1}", filePath, outputPath);
                    var host = hostManager.CreateHost(filePath, projectRelativePath, itemNamespace);

                    bool hasErrors = false;
                    host.Error += (o, eventArgs) =>
                    {
                        Log.LogError("RazorGenerator", eventArgs.ErorrCode.ToString(), helpKeyword: "", file: file.ItemSpec,
                                     lineNumber: (int)eventArgs.LineNumber, columnNumber: (int)eventArgs.ColumnNumber,
                                     endLineNumber: (int)eventArgs.LineNumber, endColumnNumber: (int)eventArgs.ColumnNumber,
                                     message: eventArgs.ErrorMessage);

                        hasErrors = true;
                    };

                    try
                    {
                        string result = host.GenerateCode();
                        if (!hasErrors)
                        {
                            // If we had errors when generating the output, don't write the file.
                            File.WriteAllText(outputPath, result);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.LogErrorFromException(exception, showStackTrace: true, showDetail: true, file: null);
                        return false;
                    }
                    if (hasErrors)
                    {
                        return false;
                    }

                    var taskItem = new TaskItem(outputPath);
                    taskItem.SetMetadata("AutoGen", "true");
                    taskItem.SetMetadata("DependentUpon", "fileName");

                    _generatedFiles.Add(taskItem);

                    var viewsGeneratedInProjectRoot = string.Format(@"{0}\", ProjectRoot) == CodeGenDirectory;
                    if (IncludeNewViewsInProject && viewsGeneratedInProjectRoot)
                    {
                        AddNewPrecompiledViewToProject(projectRelativePath, precompiledViewRelativePath, langutil.GetProjectFileExtension());
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if the file has a corresponding output code-gened file that does not require updating.
        /// </summary>
        private static bool RequiresRecompilation(string filePath, string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                return true;
            }
            return File.GetLastWriteTimeUtc(filePath) > File.GetLastWriteTimeUtc(outputPath);
        }

        private string GetNamespace(ITaskItem file, string projectRelativePath)
        {
            string itemNamespace = file.GetMetadata("CustomToolNamespace");
            if (!String.IsNullOrEmpty(itemNamespace))
            {
                return itemNamespace;
            }
            projectRelativePath = Path.GetDirectoryName(projectRelativePath);
            // To keep the namespace consistent with VS, need to generate a namespace based on the folder path if no namespace is specified.
            // Also replace any non-alphanumeric characters with underscores.
            itemNamespace = projectRelativePath.Trim(Path.DirectorySeparatorChar);
            if (String.IsNullOrEmpty(itemNamespace))
            {
                return RootNamespace;
            }

            var stringBuilder = new StringBuilder(itemNamespace.Length);
            foreach (char c in itemNamespace)
            {
                if (c == Path.DirectorySeparatorChar)
                {
                    stringBuilder.Append('.');
                }
                else if (!Char.IsLetterOrDigit(c))
                {
                    stringBuilder.Append('_');
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
            itemNamespace = stringBuilder.ToString();
            itemNamespace = _namespaceRegex.Replace(itemNamespace, "$1_$2");

            if (!String.IsNullOrEmpty(RootNamespace))
            {
                itemNamespace = RootNamespace + '.' + itemNamespace;
            }
            return itemNamespace;
        }

        private static string GetProjectRelativePath(string filePath, string projectRoot)
        {
            if (filePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return filePath.Substring(projectRoot.Length);
            }
            return filePath;
        }

        private static void EnsureDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Add a new precompiled view to the Visual Studio project.
        /// </summary>
        /// <param name="viewRelativePath">View relative path inside the project.</param>
        /// <param name="precompiledViewRelativePath">Precompiled view relative path inside the project.</param>
        /// <param name="projectExtension">Project file extension.</param>
        private void AddNewPrecompiledViewToProject(string viewRelativePath, string precompiledViewRelativePath, string projectExtension)
        {
            viewRelativePath = viewRelativePath.TrimStart(Path.DirectorySeparatorChar);
            var projectPath = string.Format(@"{0}\{1}{2}", ProjectRoot, ProjectName, projectExtension);
            var projectXml = XDocument.Load(projectPath);
            var projectNamespace = projectXml.Root.Name.Namespace;

            Func<XElement, string, bool> isNodeWithIncludeValue = ((node, includeValue) => (string)node.Attribute("Include") == includeValue);
            var projectNodes = projectXml.Nodes()
                                .OfType<XElement>()
                            .DescendantNodes()
                                .OfType<XElement>();

            var viewItem = projectNodes
                            .DescendantNodes()
                                .OfType<XElement>()
                            .FirstOrDefault(node => isNodeWithIncludeValue(node, viewRelativePath));

            var existsViewInProject = viewItem != null;
            if (existsViewInProject)
            {
                viewItem.Name = projectNamespace + "Content";
                AddOrUpdateChildNode(projectNamespace, viewItem, "Generator", "RazorGenerator");

                var lastIndexDirectorySeparator = precompiledViewRelativePath.LastIndexOf(@"\") + 1;
                var precompiledViewFileName = precompiledViewRelativePath.Substring(lastIndexDirectorySeparator);
                AddOrUpdateChildNode(projectNamespace, viewItem, "LastGenOutput", precompiledViewFileName);

                var precompiledViewItem = projectNodes
                            .DescendantNodes()
                                .OfType<XElement>()
                            .FirstOrDefault(node => isNodeWithIncludeValue(node, precompiledViewRelativePath));

                var existsPrecompiledViewInProject = precompiledViewItem != null;
                var precompiledViewItemNodeName = projectNamespace + "Compile";
                if (!existsPrecompiledViewInProject)
                {
                    precompiledViewItem = new XElement(precompiledViewItemNodeName, new XAttribute("Include", precompiledViewRelativePath));

                    AddOrUpdateChildNode(projectNamespace, precompiledViewItem, "AutoGen", "True");
                    AddOrUpdateChildNode(projectNamespace, precompiledViewItem, "DesignTime", "True");

                    lastIndexDirectorySeparator = viewRelativePath.LastIndexOf(@"\") + 1;
                    var viewFileName = viewRelativePath.Substring(lastIndexDirectorySeparator);
                    AddOrUpdateChildNode(projectNamespace, precompiledViewItem, "DependentUpon", viewFileName);

                    var lastItemGroup = projectNodes.LastOrDefault(node => node.Name.LocalName == "ItemGroup");
                    if (lastItemGroup != null)
                    {
                        lastItemGroup.Add(precompiledViewItem);
                    }
                }
                else
                {
                    precompiledViewItem.Name = precompiledViewItemNodeName;
                    AddOrUpdateChildNode(projectNamespace, precompiledViewItem, "AutoGen", "True");
                    AddOrUpdateChildNode(projectNamespace, precompiledViewItem, "DesignTime", "True");

                    lastIndexDirectorySeparator = viewRelativePath.LastIndexOf(@"\") + 1;
                    var viewFileName = viewRelativePath.Substring(lastIndexDirectorySeparator);
                    AddOrUpdateChildNode(projectNamespace, precompiledViewItem, "DependentUpon", viewFileName);
                }
                projectXml.Save(projectPath);
            }
        }

        /// <summary>
        /// Add or update a XML child node from a specific parent.
        /// </summary>
        /// <param name="nameSpace">XML namespace.</param>
        /// <param name="parentNode">XML parent node.</param>
        /// <param name="childNodeName">XML child node name.</param>
        /// <param name="childNodeValue">XML child node value.</param>
        private static void AddOrUpdateChildNode(XNamespace nameSpace, XElement parentNode, string childNodeName, string childNodeValue)
        {
            var childNode = parentNode.DescendantNodes().OfType<XElement>().FirstOrDefault(node => node.Name.LocalName == childNodeName);
            if (childNode != null)
            {
                childNode.Value = childNodeValue;
            }
            else
            {
                childNode = new XElement(nameSpace + childNodeName, childNodeValue);
                parentNode.Add(childNode);
            }
        }
    }
}