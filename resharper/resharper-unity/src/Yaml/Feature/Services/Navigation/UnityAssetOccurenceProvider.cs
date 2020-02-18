using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    [OccurrenceProvider(Priority = 10)]
    public class UnityAssetOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {
            if (findResult is UnityScriptsFindResults unityScriptsFindResults)
            {
                return new UnityScriptsOccurrence(unityScriptsFindResults.SourceFile, unityScriptsFindResults.DeclaredElementPointer, unityScriptsFindResults.AttachedElement); 
            }
            
            if (findResult is UnityInspectorFindResults unityInspectorFindResults)
            {
                return new UnityInspectorValuesOccurrence(unityInspectorFindResults.SourceFile, unityInspectorFindResults.InspectorVariableUsage, unityInspectorFindResults.DeclaredElementPointer, unityInspectorFindResults.AttachedElement); 
            }
            
            if (findResult is UnityMethodsFindResult unityMethodsFindResult)
            {
                return new UnityMethodsOccurrence(unityMethodsFindResult.SourceFile, unityMethodsFindResult.DeclaredElementPointer, unityMethodsFindResult.AttachedElement); 
            }
            
            return null;
        }
    }
}