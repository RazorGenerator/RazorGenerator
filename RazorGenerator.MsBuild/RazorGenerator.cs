using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RazorGenerator.Core;

namespace RazorGenerator.MsBuild
{
    public class RazorCodeGen : Task
    {
        private readonly List<ITaskItem> _generatedFiles = new List<ITaskItem>();

        public ITaskItem[] FilesToPrecompile { get; set; }

        public string ProjectRoot { get; set; }

        public string RootNamespace { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles
        {
            get
            {
                return _generatedFiles.ToArray();
            }
        }

        [Output]
        public string TemporaryCodeGenDirectory { get; set; }

        public override bool Execute()
        {
            if (FilesToPrecompile == null || !FilesToPrecompile.Any())
            {
                return true;
            }

            string projectRoot = String.IsNullOrEmpty(ProjectRoot) ? Directory.GetCurrentDirectory() : ProjectRoot;
            TemporaryCodeGenDirectory = Path.Combine(projectRoot, "obj", "CodeGen");

            using (var hostManager = new HostManager(projectRoot))
            {
                foreach (var file in FilesToPrecompile)
                {
                    string filePath = file.GetMetadata("FullPath");
                    string fileName = Path.GetFileName(filePath);
                    var projectRelativePath = GetProjectRelativePath(filePath, projectRoot);
                    string itemNamespace = GetNamespace(file, projectRelativePath);

                    string outputPath = Path.Combine(TemporaryCodeGenDirectory, projectRelativePath.TrimStart(Path.DirectorySeparatorChar)) + ".cs";

                    if (!RequiresRecompilation(filePath, outputPath))
                    {
                        continue;
                    }

                    EnsureDirectory(outputPath);

                    var host = hostManager.CreateHost(filePath, projectRelativePath);
                    host.DefaultNamespace = itemNamespace;

                    bool hasErrors = false;
                    host.Error += (o, eventArgs) =>
                    {
                        Log.LogError(eventArgs.ErrorMessage);
                        hasErrors = true;
                    };

                    try
                    {
                        string result = host.GenerateCode();
                        File.WriteAllText(outputPath, result);
                    }
                    catch (Exception exception)
                    {
                        Log.LogError(exception.Message);
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
            itemNamespace = projectRelativePath.Trim(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '.');

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
    }
}
