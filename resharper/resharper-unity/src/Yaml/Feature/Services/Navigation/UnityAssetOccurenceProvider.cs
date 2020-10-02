using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    [OccurrenceProvider(Priority = 10)]
    public class UnityAssetOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {

            if (findResult is UnityEventSubscriptionFindResult unityEventFindResult)
            {
                return new UnityEventSubscriptionOccurrence(unityEventFindResult.SourceFile, unityEventFindResult.DeclaredElement,
                    unityEventFindResult.AttachedElementLocation, unityEventFindResult.IsPrefabModification);
            }
            
            if (findResult is UnityScriptsFindResults unityScriptsFindResults)
            {
                var guid = unityScriptsFindResults.ScriptUsage.UsageTarget.ExternalAssetGuid;
                return new UnityScriptsOccurrence(unityScriptsFindResults.SourceFile, unityScriptsFindResults.DeclaredElementPointer,
                    unityScriptsFindResults.OwningElemetLocation, guid); 
            }
            
            if (findResult is UnityInspectorFindResult unityInspectorFindResults)
            {
                return new UnityInspectorValuesOccurrence(unityInspectorFindResults.SourceFile, unityInspectorFindResults.InspectorVariableUsage,
                    unityInspectorFindResults.DeclaredElementPointer, unityInspectorFindResults.OwningElemetLocation, unityInspectorFindResults.IsPrefabModification); 
            }
            
            if (findResult is UnityEventHandlerFindResult unityMethodsFindResult)
            {
                return new UnityEventHandlerOccurrence(unityMethodsFindResult.SourceFile, unityMethodsFindResult.DeclaredElementPointer,
                    unityMethodsFindResult.OwningElemetLocation, unityMethodsFindResult.AssetMethodUsages, unityMethodsFindResult.IsPrefabModification); 
            }
            
            return null;
        }
    }
}