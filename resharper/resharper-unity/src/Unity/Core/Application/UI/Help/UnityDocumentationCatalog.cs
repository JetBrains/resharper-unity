#nullable enable
using System;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help
{
    public class UnityDocumentationCatalog
    {
        public static UnityDocumentationCatalog ScriptReference = Create("Script Reference", "ScriptReference");
        public static UnityDocumentationCatalog Manual = Create("Manual", "Manual");

        private string CatalogName { get; }
        
        private readonly string myFilePathFormat;
        private readonly string mySearchUrlFormat;

        public UnityDocumentationCatalog(string catalogName, string filePathFormat, string searchUrlFormat)
        {
            CatalogName = catalogName;
            myFilePathFormat = filePathFormat;
            mySearchUrlFormat = searchUrlFormat;
        }

        public static UnityDocumentationCatalog Create(string name, string baseCatalog, string filePrefix = "") => new(name, $"{baseCatalog}/{filePrefix}{{0}}.html", $"/{baseCatalog}/30_search.html?q={{0}}");

        public FileSystemPath? TryGetFile(FileSystemPath documentationRoot, string subject)
        {
            if (documentationRoot.IsEmpty)
                return null;
            
            var fileSystemPath = documentationRoot/string.Format(myFilePathFormat, subject);
            return fileSystemPath.IsAbsolute && fileSystemPath.ExistsFile ? fileSystemPath : null;
        }

        public Uri GetSearchUri(Uri baseUri, string subject) => new(baseUri, string.Format(mySearchUrlFormat, subject));
    }
}