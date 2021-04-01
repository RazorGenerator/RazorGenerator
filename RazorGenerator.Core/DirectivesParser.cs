using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core
{
    internal static class DirectivesParser
    {
        private const string _globalDirectivesFileName = "razorgenerator.directives";

        /// <summary>Searches upwards from the directory containing <paramref name="razorFile"/> until it reaches <paramref name="baseDirectory"/> for any and all files named <c>razorgenerator.directives</c>.</summary>
        public static Dictionary<string,string> ReadInheritableDirectives(DirectoryInfo baseDirectory, FileInfo razorFile)
        {
            List<FileInfo> list = FindInheritableDirectivesFiles( baseDirectory, razorFile );

            list.Reverse();

            Dictionary<string,string> dict = new Dictionary<string,string>();

            foreach( FileInfo file in list )
            {
                ParseGlobalDirectives( dict, file );
            }

            return dict;
        }

        /// <summary>Searches upwards from the directory containing <paramref name="razorFile"/> until it reaches <paramref name="baseDirectory"/> for any and all files named <c>razorgenerator.directives</c>.</summary>
        private static List<FileInfo> FindInheritableDirectivesFiles(DirectoryInfo baseDirectory, FileInfo razorFile)
        {
            List<FileInfo> list = new List<FileInfo>();

            DirectoryInfo descendant = razorFile.Directory;

            while( descendant.FullName.StartsWith( baseDirectory.FullName, StringComparison.OrdinalIgnoreCase ) && descendant.FullName.Length > baseDirectory.FullName.Length && descendant.Parent != descendant )
            {
                FileInfo[] directivesFiles = descendant.GetFiles( _globalDirectivesFileName );
                if( directivesFiles.Length == 1 )
                {
                    list.Add( directivesFiles[0] );
                }

                descendant = descendant.Parent;
            }

            return list;
        }

        public static Dictionary<string, string> ParseDirectives(FileInfo razorFile, Dictionary<string,string> inheritedDirectives)
        {
            Dictionary<string,string> directives = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

            if (inheritedDirectives != null)
            {
                foreach( KeyValuePair<string,string> kvp in inheritedDirectives )
                {
                    directives.Add( kvp.Key, kvp.Value );
                }
            }

            ParseFileDirectives(directives, razorFile);

            return directives;
        }

        private static void ParseGlobalDirectives(Dictionary<string,string> directives, FileInfo directivesPath)
        {
            string fileContent = File.ReadAllText(path: directivesPath.FullName);
            ParseKeyValueDirectives(directives, fileContent);
        }

        private static void ParseFileDirectives(Dictionary<string,string> directives, FileInfo razorFile)
        {
            string inputFileContent = File.ReadAllText(razorFile.FullName); // <-- Yuk. Why are we reading the file multiple times?!
            int index = inputFileContent.IndexOf("*@", StringComparison.OrdinalIgnoreCase);
            if (inputFileContent.TrimStart().StartsWith("@*") && index != -1)
            {
                string directivesLine = inputFileContent.Substring(0, index).TrimStart('*', '@');
                ParseKeyValueDirectives(directives, directivesLine);
            }
        }

        const string _valueRegexPattern = @"[~\\\/\w\.]+";
        private static readonly Regex _directiveRegex = new Regex( @"\b(?<Key>\w+)\s*:\s*(?<Value>" + _valueRegexPattern + @"(\s*,\s*" + _valueRegexPattern + @")*)\b", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private static void ParseKeyValueDirectives(Dictionary<string, string> directives, string directivesLine)
        {
            // Captures directives as key value pairs, e.g.:
            //
            //   KEY : VALUE
            //   KEY : FOO, BAR, BAZ

            // TODO: Make this better.

            

            foreach (Match item in _directiveRegex.Matches(directivesLine))
            {
                string key   = item.Groups["Key"].Value;
                string value = item.Groups["Value"].Value;

                directives[key] = value;
            }
        }
    }
}
