using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core.Hosting
{
    public static class HostGuesser
    {
        public static bool TryGuessHost(DirectoryInfo projectRoot, string projectRelativePath, out GuessedHost host)
        {
            bool isMvcProject = IsMvcProjectDirectory( projectRoot, out RazorRuntime? runtime );
            if (isMvcProject)
            {
                Regex mvcHelperRegex = new Regex(@"(^|\\)Views(\\.*)+Helpers?", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                if (mvcHelperRegex.IsMatch(projectRelativePath))
                {
                    host = new GuessedHost( "MvcHelper", runtime.Value );
                    return true;
                }
                else
                {
                    host = new GuessedHost( "MvcView", runtime.Value );
                    return true;
                }
            }

            host = null;
            return false;
        }

        private static bool IsMvcProjectDirectory( DirectoryInfo projectRoot, out RazorRuntime? razorRuntime )
        {
            // TODO: This info could be cached (just check the last-write-time of the project file) to avoid re-reading it every time.
            try
            {
                foreach( FileInfo projectFile in GetProjectFiles( projectRoot ) )
                {
                    String projectFileText = File.ReadAllText( projectFile.FullName );
                    if( IsMvcProject( projectFileText, out razorRuntime ) )
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            razorRuntime = null;
            return false;
        }

        private static IEnumerable<FileInfo> GetProjectFiles( DirectoryInfo directory )
        {
            FileInfo[] csproj = directory.GetFiles( "*.csproj", SearchOption.TopDirectoryOnly );
            FileInfo[] vbproj = directory.GetFiles( "*.vbproj", SearchOption.TopDirectoryOnly );

            return Enumerable.Concat( csproj, vbproj );
        }

        private static readonly String[] _aspNetMvc3Hallmarks = new string[]
        {
            "System.Web.Mvc, Version=3",
            "System.Web.Razor, Version=1",
            "Microsoft.AspNet.Mvc.3" // <PackageReference>
        };

        private static readonly String[] _aspNetMvc4Hallmarks = new string[]
        {
            "System.Web.Mvc, Version=4",
            "System.Web.Razor, Version=2",
            "Microsoft.AspNet.Mvc.4"
        };

        private static readonly String[] _aspNetMvc5Hallmarks = new string[]
        {
            "System.Web.Mvc, Version=5",
            "System.Web.Razor, Version=3",
            "Microsoft.AspNet.Mvc.5"
        };

        private static bool IsMvcProject(string msBuildFileContents, out RazorRuntime? razorRuntime)
        {
            // Try MVC v5 (RazorGenerator.v3) first, then MVC v4 (RazorGenerator.v2), then MVC v3 (RazorGenerator.v1).

            if( _aspNetMvc5Hallmarks.Any( text => msBuildFileContents.IndexOf( text ) > -1 ) )
            {
                razorRuntime = RazorRuntime.Version3;
                return true;
            }
            else if( _aspNetMvc4Hallmarks.Any( text => msBuildFileContents.IndexOf( text ) > -1 ) )
            {
                razorRuntime = RazorRuntime.Version2;
                return true;
            }
            else if( _aspNetMvc3Hallmarks.Any( text => msBuildFileContents.IndexOf( text ) > -1 ) )
            {
                razorRuntime = RazorRuntime.Version1;
                return true;
            }
            else
            {
                razorRuntime = null;
                return false;
            }
        }
    }

    public class GuessedHost
    {
        public GuessedHost(string host, RazorRuntime runtime)
        {
            this.Host    = host;
            this.Runtime = runtime;
        }

        public string Host { get; }

        public RazorRuntime Runtime { get; }
    }
}
