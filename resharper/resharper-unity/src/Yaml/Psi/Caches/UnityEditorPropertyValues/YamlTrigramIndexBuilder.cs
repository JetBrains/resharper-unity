using System;
using JetBrains.Application.Threading;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Text;
using JetBrains.ReSharper.Feature.Services.Text.Trigrams;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [Language(typeof(YamlLanguage))]
    public class YamlTrigramIndexBuilder : ITrigramIndexBuilder
    {
        private readonly ILogger myLogger;

        public YamlTrigramIndexBuilder(ILogger logger)
        {
            myLogger = logger;
        }
        
        public int[] Build(IPsiSourceFile sourceFile, SeldomInterruptChecker interruptChecker)
        {
            var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IYamlFile;
            if (file == null)
                return null;
            
            using (UnsafeWriter.Cookie unsafeWriterCookie = UnsafeWriter.NewThreadLocalWriter())
            {
                TrigramIndexEntryBuilder indexEntryBuilder = new TrigramIndexEntryBuilder(unsafeWriterCookie);
                foreach (var yamlDocument in file.Documents)
                {
                    foreach (TrigramToken trigramToken in new BufferTrigramSource(yamlDocument.GetTextAsBuffer()))
                        indexEntryBuilder.Add(trigramToken);
                }

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