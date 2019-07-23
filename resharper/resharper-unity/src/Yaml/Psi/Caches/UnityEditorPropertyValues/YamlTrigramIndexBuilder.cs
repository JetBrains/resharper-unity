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
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [Language(typeof(YamlLanguage))]
    public class YamlTrigramIndexBuilder : ITrigramIndexBuilder
    {
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
                    if (UnityYamlReferenceUtil.CanContainReference(yamlDocument.GetTextAsBuffer()))
                        foreach (TrigramToken trigramToken in new BufferTrigramSource(yamlDocument.GetTextAsBuffer()))
                            indexEntryBuilder.Add(trigramToken);
                }

                UnsafeIntArray entryData = indexEntryBuilder.Build();
                return entryData.ToIntArray();
            }
        }

        public int[] Build(IDocument document, SeldomInterruptChecker interruptChecker, string displayName)
        {
            throw new NotImplementedException("Not supported operation for yaml files");
        }
    }
}