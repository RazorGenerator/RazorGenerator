using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;

namespace RazorGenerator.Core
{
    public interface IRazorHost
    {
        #region File information

        string ProjectRelativePath { get; }

        string FullPath { get; }
//      FileInfo FullPath { get; }

        #endregion

        #region Options (and System.Web.Razor.RazorEngineHost members)

        string DefaultNamespace { get; set; }

        string DefaultBaseClass { get; set; }

        ISet<string> NamespaceImports { get; }

        bool StaticHelpers { get; set; }

        bool EnableLinePragmas { get; set; }

        string DefaultClassName { get; set; }

        #endregion

        event EventHandler<GeneratorErrorEventArgs> Error;

        event EventHandler<ProgressEventArgs> Progress;

        string GenerateCode();

        CodeLanguageUtil CodeLanguageUtil { get; }

        #region Abstracted System.Web.Razor services
        // TODO: Tidy this up.

        /// <summary><c>System.Web.Razor.Parser.ParserHelpers.SanitizeClassName(string inputName)</c></summary>
        string ParserHelpers_SanitizeClassName(string inputName);

        /// <summary>Will return <see langword="null"/> if virtual-paths are not supported by the current context.</summary>
        IRazorVirtualPathUtility GetVirtualPathUtility();

        #endregion
    }

    public interface IRazorVirtualPathUtility
    {
        string ToAppRelative(string virtualPath);

        bool TryGetVirtualPathAttribute(string virtualPath, out CodeAttributeDeclaration attribute);
    }
}
