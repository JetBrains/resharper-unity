using System;
using JetBrains.Application.Threading;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Text;
using JetBrains.ReSharper.Feature.Services.Text.Trigrams;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [Language(typeof(UnityYamlLanguage))]
    public class UnityAssetTrigramIndexBuild : ITrigramIndexBuilder
    {
        public const string ASSET_REFERENCE_IDENTIFIER = "#ASSET_REFERENCE#";
        private readonly ILogger myLogger;

        public UnityAssetTrigramIndexBuild(ILogger logger)
        {
            myLogger = logger;
        }

        public int[] Build(IPsiSourceFile sourceFile, SeldomInterruptChecker interruptChecker)
        {
            using (UnsafeWriter.Cookie unsafeWriterCookie = UnsafeWriter.NewThreadLocalWriter())
            {
                TrigramIndexEntryBuilder indexEntryBuilder = new TrigramIndexEntryBuilder(unsafeWriterCookie);
                foreach (TrigramToken trigramToken in new BufferTrigramSource(new StringBuffer(ASSET_REFERENCE_IDENTIFIER)))
                    indexEntryBuilder.Add(trigramToken);
                
                UnsafeIntArray entryData = indexEntryBuilder.Build();
                return entryData.ToIntArray();
            }
        }

        public int[] Build(IDocument document, SeldomInterruptChecker interruptChecker, string displayName)
        {
            myLogger.Error("Unsupported operation for yaml file");
            return Array.Empty<int>();
        }
    }
}