using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RazorGenerator.Core
{
    internal static class DirectivesParser
    {
        private const string GlobalDirectivesFileName = "razorgenerator.directives";

        /// <summary>Loads all RazorGenerator directives specified in the Razor (cshtml/vbhtml) file in addition to searching for the first <c>RazorGenerator.directives</c> file in the directory containing the Razor file - or any of its ancestors until reaching <paramref name="baseDirectory"/> (viz. it will not search the ancestors of <paramref name="baseDirectory"/>).</summary>
        /// <param name="baseDirectory">The base (or root) directory such that parent directories are not considered part of the directory search tree for the global <c>RazorGenerator.directives</c> file.</param>
        /// <param name="razorFilePath">The file name of the Razor file (i.e. the cshtml or vbhtml file).</param>
        /// <returns>A dictionary of directive keys and values.</returns>
        public static Dictionary<string, string> ParseDirectives(string baseDirectory, string razorFilePath)
        {
            var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string globalDirectivesFileName;
            if (TryFindGlobalDirectivesFile(baseDirectory, razorFilePath, out globalDirectivesFileName))
            {
                ParseGlobalRazorGeneratorDirectives(directives, globalDirectivesFileName);
            }
            ParseFileRazorGeneratorDirectives(directives, razorFilePath);

            return directives;
        }

        /// <summary>
        /// Attempts to locate the nearest global directive file by 
        /// </summary>
        private static bool TryFindGlobalDirectivesFile(string baseDirectoryPath, string razorFilePath, out string foundGlobalDirectivesFilePath)
        {
            // Resolve paths (performed by DirectoryInfo and FileInfo constructors):
            DirectoryInfo baseDirectory = new DirectoryInfo( baseDirectoryPath );
            DirectoryInfo currentDirectory = new DirectoryInfo( Path.GetDirectoryName( razorFilePath ) );

            // Sanity-check: Verify that `razorFilePath` is a descendant of `baseDirectory`:
            if( !currentDirectory.FullName.StartsWith( baseDirectory.FullName, StringComparison.OrdinalIgnoreCase ) )
            {
                String message = String.Format( System.Globalization.CultureInfo.InvariantCulture, "The " + nameof(razorFilePath) + " value (\"{0}\") is not a descendant of " + nameof(baseDirectoryPath) + " (\"{1}\").", razorFilePath, baseDirectoryPath );
                throw new ArgumentException( message, nameof(razorFilePath) );
            }

            // While the current directory is a descendant of the base directory (assuming that matching initial substrings indicates a descendant<->ancestor relationship)...
            while( currentDirectory != null && currentDirectory.FullName.Length > baseDirectory.FullName.Length )
            {
                String candidatePath = Path.Combine( currentDirectory.FullName, GlobalDirectivesFileName );
                if( File.Exists( candidatePath ) )
                {
                    foundGlobalDirectivesFilePath = candidatePath;
                    return true;
                }
                else
                {
                    currentDirectory = currentDirectory.Parent;
                }
            }

            foundGlobalDirectivesFilePath = null;
            return false;
        }

        private static void ParseGlobalRazorGeneratorDirectives(Dictionary<string, string> directives, string globalDirectivesFileName)
        {
            // Global Razor Directives files will be short (a few hundred bytes, tops), so it's okay to read it all into memory:

            string globalDirectivesFileContent = File.ReadAllText(globalDirectivesFileName);

            ParseKeyValueDirectives(directives, globalDirectivesFileContent);
        }

        private enum State
        {
            Outside,
            AfterAtSymbol,
            InsideComment,
            AfterAsteriskInsideComment
        }

        private static void ParseFileRazorGeneratorDirectives(Dictionary<string, string> directives, string filePath)
        {
            // File Razor Generator Directives must appear as the sole content of the first Razor comment `@* comment-text *@` in the file.
            // There can be content before this comment.

            // Razor files can be large - so use a simple FSM parser to find the first Razor comment:
            // Note this is a naive parser that will return incorrect results if Razor comment delimiters are embedded within C# strings, for example, or other syntactical structures. To mitigate this the parser only checks the first 10 lines of the input file.

            StringBuilder sb = new StringBuilder();
            State state = State.Outside;
            int lineNumber = 1;
            const int MaxLinesToInspect = 10;

            // States:
            // (Outside)---'@'--->(AfterAtSymbol)----'*'--->(InsideComment)--'*'--->(AfterAsteriskInsideComment)----'@'-->+
            //  ^   |                    |                              ^                                   |             |
            //  +-<-+--------------<-----+                              +------------------<--------------<-+             |
            //  ^                                                                                                         |
            //  +-----------------<-------------------------<---------------------------------<-------------------<-------+

            using( StreamReader reader = new StreamReader( filePath ) )
            {
                int nc;
                char c;
                while( ( nc = reader.Read() ) > -1 )
                {
                    c = (char)nc;

                    switch( state )
                    {
                        case State.Outside:

                            if( c == '@' )
                            {
                                state = State.AfterAtSymbol;
                            }

                            break;
                        case State.AfterAtSymbol:

                            if( c == '*' )
                            {
                                state = State.InsideComment;
                            }
                            else
                            {
                                state = State.Outside;
                            }

                            break;
                        case State.InsideComment:

                            if( c == '*' )
                            {
                                state = State.AfterAsteriskInsideComment;
                            }
                            else
                            {
                                sb.Append( c );
                            }

                            break;
                        case State.AfterAsteriskInsideComment:

                            if( c == '@' )
                            {
                                state = State.Outside;
                            }
                            else
                            {
                                sb.Append( c );
                                state = State.InsideComment;
                            }

                            break;
                    }

                    if( c == '\n' ) lineNumber++;
                    if( lineNumber > MaxLinesToInspect && state == State.Outside ) break;
                }
            }

            string directivesText = sb.ToString();

            if( !String.IsNullOrWhiteSpace( directivesText ) )
            {
                ParseKeyValueDirectives(directives, directivesText);
            }
        }

        private static readonly Regex _directiveKeyValuePattern = CreateDirectiveRegex();

        private static Regex CreateDirectiveRegex()
        {
            // Values: at least one word character or tilde, backslash or forwardslash
            // Keys  : at least one word character, must have a colon (but can have any whitespace around the colon.)
            // Pairs : Keys are required. Values are optional. Values  be comma-separated.

            // Quick regex reference:
            // \s - class - any whitespace character
            // \w - class - any word character
            // \S - class - any non-whitespace character
            // \W - class - any non-word character
            // \b - anchor - match must occur on a word-nonword boundary    , e.g. `\b\w+\s\w+\b` + "them theme them them"    -> [ "them theme", "them them" ]
            // \B - anchor - match must not occur on a word-nonword boundary, e.g. `\Bend\w*\b`   + "end sends endure lender" -> [ "ends", "ender" ]

            const string valuePattern = @"[~\\\/\w\.]+"; // 
            const string keyPattern   = @"\b(?<Key>\w+)\s*:\s*";
            const string pairPattern  = keyPattern + @"(?<Value>" + valuePattern + @"(\s*,\s*" + valuePattern + @")*)\b";

            return new Regex( pairPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture );
        }

        public static void ParseKeyValueDirectives(Dictionary<string, string> directives, string directivesText)
        {
            // Captures directives as key value pairs, e.g.:
            //
            //   KEY : VALUE
            //   KEY : FOO, BAR, BAZ

            MatchCollection matches = _directiveKeyValuePattern.Matches( directivesText );
            foreach( Match match in matches )
            {
                String key   = match.Groups["Key"  ].Value;
                String value = match.Groups["Value"].Value;

                directives[ key ] = value;
            }
        }
    }
}
