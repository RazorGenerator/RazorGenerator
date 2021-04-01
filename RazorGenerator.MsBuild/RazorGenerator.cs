using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using RazorGenerator.Core;

namespace RazorGenerator.MsBuild
{
    public class RazorCodeGen : Task
    {
        private static readonly Regex _namespaceRegex = new Regex(@"($|\.)(\d)");
        private readonly List<ITaskItem> _generatedFiles = new List<ITaskItem>();

        public ITaskItem[] FilesToPrecompile { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        public string RootNamespace { get; set; }

        [Required]
        public string CodeGenDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles
        {
            get
            {
                return this._generatedFiles.ToArray();
            }
        }

        public override bool Execute()
        {
            try
            {
                return this.ExecuteCore();
            }
            catch (Exception ex)
            {
                this.Log.LogError(ex.Message);
            }
            return false;
        }

        private bool ExecuteCore()
        {
            if (this.FilesToPrecompile == null || !this.FilesToPrecompile.Any())
            {
                return true;
            }

            DirectoryInfo projectRootDir;
            {
                if(String.IsNullOrEmpty(this.ProjectRoot))
                {
                    projectRootDir = new DirectoryInfo(Directory.GetCurrentDirectory());
                }
                else
                {
                    projectRootDir = new DirectoryInfo(this.ProjectRoot);
                }
            }

            using (RazorHostManager hostManager = new RazorHostManager(baseDirectory: projectRootDir))//, loadExtensions: true, defaultRuntime: RazorRuntime.Version3, assemblyDirectory: ))
            {
                foreach (ITaskItem file in this.FilesToPrecompile)
                {
                    FileInfo filePath            = new FileInfo(file.GetMetadata("FullPath"));
                    string   fileName            = filePath.Name;
                    string   projectRelativePath = GetProjectRelativePath(filePath, projectRootDir);
                    string   itemNamespace       = this.GetNamespace(file, projectRelativePath);

                    CodeLanguageUtil langutil = CodeLanguageUtil.GetLanguageUtilFromFileName(filePath);

                    string outputPath = Path.Combine(this.CodeGenDirectory, projectRelativePath.TrimStart(Path.DirectorySeparatorChar)) + langutil.GetCodeFileExtension();
                    if (!RequiresRecompilation(filePath, outputPath))
                    {
                        this.Log.LogMessage(MessageImportance.Low, "Skipping file {0} since {1} is already up to date", filePath, outputPath);
                        continue;
                    }
                    EnsureDirectory(outputPath);

                    this.Log.LogMessage(MessageImportance.Low, "Precompiling {0} at path {1}", filePath, outputPath);
                    IRazorHost host = hostManager.CreateHost(filePath, projectRelativePath, itemNamespace);

                    bool hasErrors = false;
                    host.Error += (o, eventArgs) =>
                    {
                        this.Log.LogError(
                            subcategory    : "RazorGenerator",
                            errorCode      : eventArgs.ErorrCode.ToString(),
                            helpKeyword    : "",
                            file           : file.ItemSpec,
                            lineNumber     : (int)eventArgs.LineNumber,
                            columnNumber   : (int)eventArgs.ColumnNumber,
                            endLineNumber  : (int)eventArgs.LineNumber,
                            endColumnNumber: (int)eventArgs.ColumnNumber,
                            message        : eventArgs.ErrorMessage
                        );

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
                        this.Log.LogErrorFromException(exception, showStackTrace: true, showDetail: true, file: null);
                        return false;
                    }
                    if (hasErrors)
                    {
                        return false;
                    }

                    TaskItem taskItem = new TaskItem(outputPath);
                    taskItem.SetMetadata("AutoGen", "true");
                    taskItem.SetMetadata("DependentUpon", "fileName");

                    this._generatedFiles.Add(taskItem);
                }
            }
            return true;
        }

        private static bool RequiresRecompilation(FileInfo fileInfo, string outputPath)
        {
            fileInfo.Refresh();

            bool isUpToDate = fileInfo.Exists && fileInfo.LastWriteTimeUtc <= File.GetLastWriteTimeUtc(outputPath);
            return !isUpToDate;
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
                return this.RootNamespace;
            }

            StringBuilder stringBuilder = new StringBuilder(itemNamespace.Length);
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

            if (!String.IsNullOrEmpty(this.RootNamespace))
            {
                itemNamespace = this.RootNamespace + '.' + itemNamespace;
            }
            return itemNamespace;
        }

        private static string GetProjectRelativePath(FileInfo filePath, DirectoryInfo projectRoot)
        {
            // HACK: Is there a better, more reliable (and *correct*?) way of comparing paths in Windows, as you can with inodes in *nix?

            String filePathStr        = filePath.FullName;
            String projectRootPathStr = projectRoot.FullName;

            if( filePathStr.StartsWith( projectRootPathStr, StringComparison.OrdinalIgnoreCase ) )
            {
                return filePathStr.Substring( projectRootPathStr.Length );
            }

            return filePathStr;
        }

        private static void EnsureDirectory(string filePath)
        {
            String directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
