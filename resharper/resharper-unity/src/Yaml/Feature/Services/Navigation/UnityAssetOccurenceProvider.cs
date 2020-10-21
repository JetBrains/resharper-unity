using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
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
            
            if (findResult is UnityScriptsFindResults scriptFindResult) return CreateScriptOccurence(scriptFindResult);

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

        [CanBeNull]
        private static IOccurrence CreateScriptOccurence([NotNull] UnityScriptsFindResults unityScriptsFindResults)
        {
            var file = unityScriptsFindResults.SourceFile;
            if (file is null) return null;
            var declaredElementPointer = unityScriptsFindResults.DeclaredElementPointer;
            if (declaredElementPointer is null) return null;
            switch (unityScriptsFindResults.ScriptUsage)
            {
                case AssetScriptUsages assetScriptUsage:
                    var guid = assetScriptUsage.UsageTarget.ExternalAssetGuid;
                    var owningElementLocation = unityScriptsFindResults.OwningElemetLocation;
                    return new UnityScriptsOccurrence(file, declaredElementPointer, owningElementLocation, guid);
                case AnimatorStateScriptUsage animatorStateUsage:
                    return new UnityAnimatorScriptOccurence(file, declaredElementPointer, animatorStateUsage);
                case AnimatorStateMachineScriptUsage animatorStateMachineUsage:
                    return new UnityAnimatorScriptOccurence(file, declaredElementPointer, animatorStateMachineUsage);
            }
            return null;
        }
    }
}