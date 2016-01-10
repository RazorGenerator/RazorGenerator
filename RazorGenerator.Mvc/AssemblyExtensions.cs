using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace RazorGenerator.Mvc
{
    internal static class AssemblyExtensions
    {
        public static DateTime GetLastWriteTimeUtc(this Assembly assembly, DateTime fallback)
        {
            string assemblyLocation = null;
            try
            {
                assemblyLocation = assembly.Location;
            }
            catch (SecurityException)
            {
                // In partial trust we may not be able to read assembly.Location. In which case, we'll try looking at assembly.CodeBase
                Uri uri;
                if (!String.IsNullOrEmpty(assembly.CodeBase) && Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out uri) && uri.IsFile)
                {
                    assemblyLocation = uri.LocalPath;
                }
            }

            if (String.IsNullOrEmpty(assemblyLocation))
            {
                // If we are unable to read the filename, return fallback value.
                return fallback;
            }

            DateTime timestamp;
            try
            {
                timestamp = File.GetLastWriteTimeUtc(assemblyLocation);
                if (timestamp.Year == 1601)
                {
                    // 1601 is returned if GetLastWriteTimeUtc for some reason cannot read the timestamp.
                    timestamp = fallback;
                }
            }
            catch (UnauthorizedAccessException)
            {
                timestamp = fallback;
            }
            catch (PathTooLongException)
            {
                timestamp = fallback;
            }
            catch (NotSupportedException)
            {
                timestamp = fallback;
            }
            return timestamp;
        }
    }
}