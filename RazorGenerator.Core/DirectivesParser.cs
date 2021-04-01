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

        public static Dictionary<string, string> ParseDirectives(DirectoryInfo baseDirectory, FileInfo fullPath)
        {
            Dictionary<string, string> directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (TryFindGlobalDirectivesFile(baseDirectory, fullPath, out FileInfo directivesPath))
            {
                ParseGlobalDirectives(directives, directivesPath);
            }
            ParseFileDirectives(directives, fullPath);

            return directives;
        }

        /// <summary>
        /// Attempts to locate the nearest global directive file by 
        /// </summary>
        private static bool TryFindGlobalDirectivesFile(DirectoryInfo baseDirectory, FileInfo nonGlobalDirectivesFile, out FileInfo globalDirectiveFile)
        {
            DirectoryInfo nonGlobalDirectivesFileDirectory = nonGlobalDirectivesFile.Directory;

            /*
            baseDirectory = baseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var directivesDirectory = Path.GetDirectoryName(fullPath).TrimEnd(Path.DirectorySeparatorChar);
            while (directivesDirectory != null && directivesDirectory.Length >= baseDirectory.Length)
            {
                path = Path.Combine(directivesDirectory, GlobalDirectivesFileName);
                if (File.Exists(path))
                {
                    return true;
                }
                directivesDirectory = Path.GetDirectoryName(directivesDirectory).TrimEnd(Path.DirectorySeparatorChar);
            }
            path = null;
            return false;
            */

            globalDirectiveFile = baseDirectory
                .EnumerateFiles(searchPattern: _globalDirectivesFileName, searchOption: SearchOption.AllDirectories)
                .Where(fi => fi.FullName.StartsWith(nonGlobalDirectivesFileDirectory.FullName, StringComparison.OrdinalIgnoreCase)) // hmm, what about `/` vs `\` in paths? this needs to be normalized in order to do any meaningful comparison.
                .FirstOrDefault();

            return globalDirectiveFile != null;
        }

        private static void ParseGlobalDirectives(Dictionary<string, string> directives, FileInfo directivesPath)
        {
            var fileContent = File.ReadAllText(path: directivesPath.FullName);
            ParseKeyValueDirectives(directives, fileContent);
        }

        private static void ParseFileDirectives(Dictionary<string, string> directives, FileInfo filePath)
        {
            var inputFileContent = File.ReadAllText(filePath.FullName);
            int index = inputFileContent.IndexOf("*@", StringComparison.OrdinalIgnoreCase);
            if (inputFileContent.TrimStart().StartsWith("@*") && index != -1)
            {
                string directivesLine = inputFileContent.Substring(0, index).TrimStart('*', '@');
                ParseKeyValueDirectives(directives, directivesLine);
            }
        }

        private static void ParseKeyValueDirectives(Dictionary<string, string> directives, string directivesLine)
        {
            // Captures directives as key value pairs, e.g.:
            //
            //   KEY : VALUE
            //   KEY : FOO, BAR, BAZ

            // TODO: Make this better.

            const string valueRegexPattern = @"[~\\\/\w\.]+";
            var regex = new Regex(@"\b(?<Key>\w+)\s*:\s*(?<Value>" + valueRegexPattern + @"(\s*,\s*" + valueRegexPattern + @")*)\b", RegexOptions.ExplicitCapture);

            foreach (Match item in regex.Matches(directivesLine))
            {
                var key = item.Groups["Key"].Value;
                var value = item.Groups["Value"].Value;

                directives[key] = value;
            }
        }
    }
}
