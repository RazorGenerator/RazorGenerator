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

namespace RazorGenerator.MsBuild
{
    public class RazorCodeGen : Task
    {
        private static readonly Regex _namespaceRegex = new Regex(@"($|\.)(\d)");

        private readonly List<ITaskItem> generatedFiles = new List<ITaskItem>();

        #region MSBuild Target <RazorCodeGen> attributes:

        /// <summary>This is set by &lt;RazorCodeGen FilesToPrecompile=&quot;&quot;&gt; in RazorGenerator.msbuild.targets</summary>
        public ITaskItem[] FilesToPrecompile { get; set; }

        /// <summary>This is set by &lt;RazorCodeGen FilesToPrecompile=&quot;&quot;&gt; in RazorGenerator.msbuild.targets</summary>
        [Required]
        public string ProjectRoot { get; set; }

        public string RootNamespace { get; set; }

        [Required]
        public string CodeGenDirectory { get; set; }

        /// <summary>This is set by &lt;RazorGeneratorMSBuildDebug&gt; in RazorGenerator.msbuild.targets</summary>
        public string EnableDebug { get; set; }

        #endregion

        [Output]
        public ITaskItem[] GeneratedFiles
        {
            get
            {
                return this.generatedFiles.ToArray();
            }
        }

        public override bool Execute()
        {
            if( this.ShouldWaitForDebugger() )
            {
                this.Log.LogMessage( "Waiting for debugger..." );

                // Wait for up to 30 seconds.

                Stopwatch sw = Stopwatch.StartNew();

                while( !Debugger.IsAttached )
                {
                    //if( Debugger.Launch() )
                    //{
                    //}
                    //
                    //Debugger.Break();

                    if( sw.Elapsed.TotalSeconds > 30 )
                    {
                        this.Log.LogMessage( "No debugger attached within 30 seconds. Aborting." );
                        return false;
                    }

                    System.Threading.Thread.Sleep( 500 );
                
                    this.Log.LogMessage( "Waiting for debugger..." );
                }
            }

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

        private bool ShouldWaitForDebugger()
        {
            const String DebugMSBuildPropertyName = "RazorGeneratorMSBuildDebug";
            const String DebugEnvVarName          = "RAZORGENERATOR_WAIT_FOR_DEBUGGER";

            String razorGeneratorMSBuildDebugValue = this.EnableDebug ?? String.Empty;
            String razorGeneratorMSBuildDebugEnv   = Environment.GetEnvironmentVariable( DebugEnvVarName ) ?? String.Empty;

            this.Log.LogMessage( "The MSBuild property <" + DebugMSBuildPropertyName + "> == \"{0}\".", razorGeneratorMSBuildDebugValue );
            this.Log.LogMessage( "The " + DebugEnvVarName + " environment variable == \"{0}\".", razorGeneratorMSBuildDebugEnv );

            return ParseAsBoolean( razorGeneratorMSBuildDebugValue ) || ParseAsBoolean( razorGeneratorMSBuildDebugEnv );
        }

        private static bool ParseAsBoolean( string text )
        {
            if( !String.IsNullOrWhiteSpace( text ) )
            {
                text = text.Trim();

                return
                    "1"   .Equals( text, StringComparison.OrdinalIgnoreCase ) ||
                    "true".Equals( text, StringComparison.OrdinalIgnoreCase ) ||
                    "yes" .Equals( text, StringComparison.OrdinalIgnoreCase ) ||
                    "t"   .Equals( text, StringComparison.OrdinalIgnoreCase ) ||
                    "y"   .Equals( text, StringComparison.OrdinalIgnoreCase );
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

            // Assume the RazorGenerator.Core.vN.dll assembly file is in the same directory, or a descendant, as the current assembly.
            // TODO: Allow the user to specify the RazorRuntime version in an MSBuild <property>.
            FileInfo razorGeneratorVersionAssemblyFile = MSBuildRazorGeneratorAssemblyLocator.LocateRazorGeneratorAssembly( RazorRuntime.Version3 );
            if( razorGeneratorVersionAssemblyFile is null )
            {
                this.Log.LogError( message: "Couldn't locate RazorGenerator.Core.dll" );
                return false;
            }

            using (RazorHostManager hostManager = new RazorHostManager( baseDirectory: projectRootDir, loadExtensions: true, razorGeneratorVersionAssemblyFile ) )
            {
                foreach (ITaskItem file in this.FilesToPrecompile)
                {
                    bool ok = this.ExecuteCoreFile( hostManager, projectRootDir, file );
                    if( !ok ) return false;
                }
            }
            return true;
        }

        private FileInfo GetRazorGeneratorOutputFileName(FileInfo filePath, string projectRelativePath)
        {
            projectRelativePath = projectRelativePath.TrimStart( Path.DirectorySeparatorChar );

            CodeLanguageUtil langutil = CodeLanguageUtil.GetLanguageUtilFromFileName(filePath);

            if( this.CodeGenDirectory == "." )
            {
                String path = Path.Combine( filePath.Directory.FullName, filePath.Name );
                path = Path.ChangeExtension( path, extension: langutil.GetCodeFileExtension() );

                // hmmm?
                if( path.EndsWith( ".cs", StringComparison.OrdinalIgnoreCase ) && !path.EndsWith( ".generated.cs", StringComparison.OrdinalIgnoreCase ) )
                {
                    path = Path.ChangeExtension( path, extension: ".generated.cs" );
                }
                else if( path.EndsWith( ".vb", StringComparison.OrdinalIgnoreCase ) && !path.EndsWith( ".generated.vb", StringComparison.OrdinalIgnoreCase ) )
                {
                    path = Path.ChangeExtension( path, extension: ".generated.cs" );
                }

                return new FileInfo( path );
            }
            else
            {
                String path = Path.Combine( this.CodeGenDirectory, projectRelativePath ) + langutil.GetCodeFileExtension();
                return new FileInfo( path );
            }
        }

        private bool ExecuteCoreFile( RazorHostManager hostManager, DirectoryInfo projectRootDir, ITaskItem file )
        {
            FileInfo filePath            = new FileInfo(file.GetMetadata("FullPath"));
            string   fileName            = filePath.Name;
            string   projectRelativePath = GetProjectRelativePath(filePath, projectRootDir);
            string   itemNamespace       = this.GetNamespace(file, projectRelativePath);
            FileInfo outputPath          = this.GetRazorGeneratorOutputFileName( filePath, projectRelativePath );

            if (!RequiresRecompilation(filePath, outputPath.FullName))
            {
                this.Log.LogMessage(MessageImportance.Low, "Skipping file {0} since {1} is already up to date", filePath, outputPath);
                return true;
            }
            EnsureDirectory(outputPath.FullName);

            this.Log.LogMessage(MessageImportance.Low, "Precompiling {0} at path {1}", filePath.FullName, outputPath.FullName);
            IRazorHost host = hostManager.CreateHost(filePath, projectRelativePath, itemNamespace);

            bool hasErrors = false;
            host.Error += (o, eventArgs) =>
            {
                this.Log.LogError(
                    subcategory    : "RazorGenerator",
                    errorCode      : eventArgs.ErrorCode.ToString(),
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
                    File.WriteAllText( path: outputPath.FullName, contents: result );
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

            TaskItem taskItem = new TaskItem(outputPath.FullName);
            taskItem.SetMetadata("AutoGen", "true");
            taskItem.SetMetadata("DependentUpon", "fileName");

            this.generatedFiles.Add(taskItem);
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
                    _ = stringBuilder.Append('.');
                }
                else if (Char.IsLetterOrDigit(c))
                {
                    _ = stringBuilder.Append(c);
                }
                else
                {
                    _ = stringBuilder.Append('_');
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
                _ = Directory.CreateDirectory(directory);
            }
        }
    }
}
