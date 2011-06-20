using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RazorGenerator.Core;

namespace RazorGenerator.MsBuild {
    public class RazorCodeGen : Task {
        private readonly List<ITaskItem> _generatedFiles = new List<ITaskItem>();

        public ITaskItem[] FilesToPrecompile { get; set; }

        public string RootNamespace { get; set; }

        public string ProjectRoot { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles {
            get {
                return _generatedFiles.ToArray();
            }
        }

        [Output]
        public string TemporaryCodeGenDirectory { get; set; }

        public override bool Execute() {
            string projectRoot = String.IsNullOrEmpty(ProjectRoot) ? Directory.GetCurrentDirectory() : ProjectRoot;
            TemporaryCodeGenDirectory = Path.Combine(projectRoot, "obj", "CodeGen");

            using (var hostManager = new HostManager(projectRoot)) {
                foreach (var file in FilesToPrecompile) {
                    string itemNamespace = file.GetMetadata("CustomToolNamespace");
                    if (String.IsNullOrEmpty(itemNamespace)) {
                        itemNamespace = RootNamespace;
                    }
                    string filePath = file.ItemSpec;
                    string fileName = Path.GetFileName(filePath);
                    var projectRelativePath = GetProjectRelativePath(filePath, projectRoot);

                    var host = hostManager.CreateHost(filePath, projectRelativePath);
                    host.DefaultNamespace = itemNamespace;

                    if (!String.IsNullOrEmpty(itemNamespace)) {
                        string outputPath = Path.Combine(TemporaryCodeGenDirectory, projectRelativePath) + ".cs";
                        EnsureDirectory(outputPath);
                        File.WriteAllText(outputPath, host.GenerateCode());

                        var taskItem = new TaskItem(outputPath);
                        taskItem.SetMetadata("AutoGen", "true");
                        taskItem.SetMetadata("DependentUpon", "fileName");

                        _generatedFiles.Add(taskItem);
                    }
                }
            }
            return true;
        }

        private static string GetProjectRelativePath(string filePath, string projectRoot) {
            if (filePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)) {
                return filePath.Substring(projectRoot.Length);
            }
            return filePath;
        }

        private static void EnsureDirectory(string filePath) {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
