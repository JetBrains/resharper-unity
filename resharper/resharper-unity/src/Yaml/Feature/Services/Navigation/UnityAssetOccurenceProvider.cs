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

            if (findResult is UnityEventFindResult unityEventFindResult)
            {
                return new UnityEventOccurrence(unityEventFindResult.SourceFile, unityEventFindResult.DeclaredElement,
                    unityEventFindResult.AttachedElementLocation, unityEventFindResult.IsPrefabModification);
            }
            
            if (findResult is UnityScriptsFindResults unityScriptsFindResults)
            {
                var guid = unityScriptsFindResults.AssetUsage.ExternalDependency.ExternalAssetGuid;
                return new UnityScriptsOccurrence(unityScriptsFindResults.SourceFile, unityScriptsFindResults.DeclaredElementPointer,
                    unityScriptsFindResults.AttachedElementLocation, guid); 
            }
            
            if (findResult is UnityInspectorFindResults unityInspectorFindResults)
            {
                return new UnityInspectorValuesOccurrence(unityInspectorFindResults.SourceFile, unityInspectorFindResults.InspectorVariableUsage,
                    unityInspectorFindResults.DeclaredElementPointer, unityInspectorFindResults.AttachedElementLocation); 
            }
            
            if (findResult is UnityMethodsFindResult unityMethodsFindResult)
            {
                return new UnityMethodsOccurrence(unityMethodsFindResult.SourceFile, unityMethodsFindResult.DeclaredElementPointer,
                    unityMethodsFindResult.AttachedElementLocation, unityMethodsFindResult.AssetMethodData, unityMethodsFindResult.IsPrefabModification); 
            }
            
            return null;
        }
    }
}