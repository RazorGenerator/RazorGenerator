using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core
{
    internal static class DirectivesParser
    {
        private const string GlobalDirectivesFileName = "razorgenerator.directives";

        public static Dictionary<string, string> ParseDirectives(string baseDirectory, string fullPath)
        {
            var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParseGlobalDirectives(directives, baseDirectory);
            ParseFileDirectives(directives, fullPath);

            return directives;
        }

        private static void ParseGlobalDirectives(Dictionary<string, string> directives, string baseDirectory)
        {
            var globalDirectivesFile = Path.Combine(baseDirectory, GlobalDirectivesFileName);
            if (File.Exists(globalDirectivesFile))
            {
                var fileContent = File.ReadAllText(globalDirectivesFile);
                ParseKeyValueDirectives(directives, fileContent);
            }
        }

        private static void ParseFileDirectives(Dictionary<string, string> directives, string filePath)
        {
            var inputFileContent = File.ReadAllText(filePath);
            int index = inputFileContent.IndexOf("*@", StringComparison.OrdinalIgnoreCase);
            if (inputFileContent.TrimStart().StartsWith("@*") && index != -1)
            {
                string directivesLine = inputFileContent.Substring(0, index).TrimStart('*', '@');
                ParseKeyValueDirectives(directives, directivesLine);
            }
        }

        private static void ParseKeyValueDirectives(Dictionary<string, string> directives, string directivesLine)
        {
            // TODO: Make this better.
            var regex = new Regex(@"\b(?<Key>\w+)\s*:\s*(?<Value>[~\\\/\w\.]+)\b", RegexOptions.ExplicitCapture);
            foreach (Match item in regex.Matches(directivesLine))
            {
                var key = item.Groups["Key"].Value;
                var value = item.Groups["Value"].Value;

                directives.Add(key, value);
            }
        }
    }
}
