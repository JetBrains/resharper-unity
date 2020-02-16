using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OccurrenceProvider(Priority = 10)]
    public class UnityAssetOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {
            if (findResult is UnityAssetFindResult unityAssetFindResult)
            {
                return new UnityAssetOccurrence(unityAssetFindResult.SourceFile, unityAssetFindResult.DeclaredElementPointer, 
                    unityAssetFindResult.TextRange, unityAssetFindResult.Parent); 
            }
            
            return null;
        }
    }
}