using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Extension;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public partial class UnityEventsElementContainer
    {
        #region PREFAB_MODIFICATIONS_SUPPORT
        
        /// <summary>
        /// Prefab modification could add usage to method without using its name, e.g changing m_Target field in m_Calls array element or m_Mode.
        /// If file has m_Target modification and does not have any m_MethodName modification, we will add it to that counting set and
        /// for each find usages request for method we will scan all files from that set for usages
        ///
        /// Possible improvment:
        /// 1. Store for each IPsiSourceFile collection of m_Target references
        /// 2. GetPossibleFilesWithUsage will resolve m_Target to real ScriptComponentHierarchy with m_Script guid
        /// 3. GetPossibleFilesWithUsage will check that associated with guid type element is derived from method's type element and process only in that case
        /// NB : resolve to real ScriptComponentHierarchy will be cached in PrefabImportCache for stripped elements or will be simply available in current scene hierarchy
        /// </summary>
        private readonly CountingSet<IPsiSourceFile> myFilesToCheckForUsages = new CountingSet<IPsiSourceFile>();
        
        // both collection could be removed and replaced by pointer to UnityEventsDataElement with coressponding sourceFile
        // NB: LocalReference == pointer to source file too.
        private readonly Dictionary<IPsiSourceFile, ImportedUnityEventData> myImportedUnityEventDatas = new Dictionary<IPsiSourceFile, ImportedUnityEventData>();
        #endregion
        
        
        private ImportedUnityEventData ProcessPrefabModifications(IPsiSourceFile currentFile, AssetDocument document)
        {
            var result = new ImportedUnityEventData();
            var assetMethodUsagesSet = new Dictionary<int, (LocalReference, AssetMethodUsagesData)>();
            if (document.HierarchyElement is IPrefabInstanceHierarchy prefabInstanceHierarchy)
            {
                var assetMethodDataToModifiedFields = new OneToSetMap<(LocalReference, string, int), string>();
                foreach (var modification in prefabInstanceHierarchy.PrefabModifications)
                {
                    if (!(modification.Target is ExternalReference externalReference))
                        continue;
                    
                    if (!modification.PropertyPath.Contains("m_PersistentCalls"))
                        continue;
                    
                    var location = new LocalReference(currentFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), PrefabsUtil.GetImportedDocumentAnchor(prefabInstanceHierarchy.Location.LocalDocumentAnchor, externalReference.LocalDocumentAnchor));
                    var parts = modification.PropertyPath.Split('.');
                    var unityEventName = parts[0];

                    var dataPart = parts.FirstOrDefault(t => t.StartsWith("data"));
                    if (dataPart == null)
                        continue;
                    
                    if (!int.TryParse(dataPart.RemoveStart("data[").RemoveEnd("]"), out var index))
                        continue;
                    
                    if (!assetMethodUsagesSet.TryGetValue(index, out var value))
                    {
                        value = new (location, new AssetMethodUsagesData());
                        assetMethodUsagesSet.Add(index, value);
                    }
                    
                    assetMethodUsagesSet[index].Item2.unityEventName = unityEventName;
                    result.UnityEventToModifiedIndex.Add((location, unityEventName), index);
                    
                    var last = parts.Last();
                    if (last.Equals("m_MethodName") && modification.Value is AssetSimpleValue assetSimpleValue)
                    {
                        result.AssetMethodNameInModifications.Add(assetSimpleValue.SimpleValue);
                        assetMethodUsagesSet[index].Item2.methodName = assetSimpleValue.SimpleValue;
                        assetMethodUsagesSet[index].Item2.textRangeOwnerPsiPersistentIndex = modification.ValueRange;
                    }
                    else if (last.Equals("m_Target") && modification.ObjectReference is ExternalReference er)
                        assetMethodUsagesSet[index].Item2.targetReference = er;
                    else if (last.Equals("m_Mode") && modification.Value is AssetSimpleValue modeSimpleValue)
                        assetMethodUsagesSet[index].Item2.mode = GetEventHandlerArgumentMode(modeSimpleValue.SimpleValue);
                    else if (last.Equals("m_ObjectArgumentAssemblyTypeName") && modification.Value is AssetSimpleValue objectArgumentAssemblyTypeNameSimpleValue)
                        assetMethodUsagesSet[index].Item2.type = objectArgumentAssemblyTypeNameSimpleValue.SimpleValue?.Split(',').FirstOrDefault(); // the logic here is simpler then in UnityEventsElementContainer.cs

                    assetMethodUsagesSet[index].Item2.textRangeOwner =
                        currentFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null");
                    assetMethodDataToModifiedFields.Add((location, unityEventName, index), last);
                }
                
                foreach (var (_, set) in assetMethodDataToModifiedFields)
                    if (!set.Contains("m_MethodName"))
                        result.HasEventModificationWithoutMethodName = true;
            }

            foreach (var valueTuple in assetMethodUsagesSet)
            {
                result.AssetMethodUsagesSet.Add(valueTuple.Value.Item1, valueTuple.Value.Item2.ToAssetMethodUsages());
            }

            return result;
        }

        private void DropPrefabModifications(IPsiSourceFile sourceFile, UnityEventsDataElement element)
        {
            foreach (var (unityEvent, _) in element.ImportedUnityEventData.UnityEventToModifiedIndex)
            {
                myUnityEventsWithModifications.Remove(unityEvent.EventName);
                myUnityEventNameToSourceFiles.Remove(unityEvent.EventName, sourceFile);
            }

            foreach (var assetMethodNameInModification in element.ImportedUnityEventData.AssetMethodNameInModifications)
            {
                myMethodNameToFilesWithPossibleUsages.Remove(assetMethodNameInModification, sourceFile);
            }

            if (element.ImportedUnityEventData.HasEventModificationWithoutMethodName)
                myFilesToCheckForUsages.Remove(sourceFile);
            
            myImportedUnityEventDatas.Remove(sourceFile);
        }

        private void MergePrefabModifications(IPsiSourceFile sourceFile, UnityEventsDataElement element)
        {
            foreach (var (unityEvent, _) in element.ImportedUnityEventData.UnityEventToModifiedIndex)
            {
                myUnityEventsWithModifications.Add(unityEvent.EventName);
                myUnityEventNameToSourceFiles.Add(unityEvent.EventName, sourceFile);
            }

            foreach (var assetMethodNameInModification in element.ImportedUnityEventData.AssetMethodNameInModifications)
            {
                myMethodNameToFilesWithPossibleUsages.Add(assetMethodNameInModification, sourceFile);
            }

            if (element.ImportedUnityEventData.HasEventModificationWithoutMethodName)
                myFilesToCheckForUsages.Add(sourceFile);
            
            myImportedUnityEventDatas.Add(sourceFile, element.ImportedUnityEventData);
        }

        private IEnumerable<(LocalReference owningScriptLocation, AssetMethodUsages methodData)> GetImportedAssetMethodDataFor(IPsiSourceFile psiSourceFile)
        {
            if (myImportedUnityEventDatas.TryGetValue(psiSourceFile, out var importedUnityEventData))
            {
                foreach (var assetMethodUsages in importedUnityEventData.AssetMethodUsagesSet)
                {
                    yield return (assetMethodUsages.Key, assetMethodUsages.Value);
                }
                
                foreach (var ((location, unityEventName), modifiedEvents) in importedUnityEventData.UnityEventToModifiedIndex)
                {
                    var script = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(location, true) as IScriptComponentHierarchy;
                    // missed script
                    if (script == null)
                        continue;
                    
                    var scriptType = AssetUtils.GetTypeElementFromScriptAssetGuid(mySolution, script.ScriptReference.ExternalAssetGuid);
                    var field = scriptType?.GetMembers().FirstOrDefault(t => t is IField f && AssetUtils.GetAllNamesFor(f).Contains(unityEventName)) as IField;
                    if (field == null)
                        continue;

                    var names = AssetUtils.GetAllNamesFor(field).ToJetHashSet();
                    var modifications = script.ImportUnityEventData(this, names);

                    foreach (var index in modifiedEvents)
                    {
                        Assertion.Assert(index < modifications.Count, "index < modifications.Count");
                        var result = AssetMethodUsages.TryCreateAssetMethodFromModifications(location, unityEventName, modifications[index]);
                        if (result != null)
                            yield return (location, result);
                    }
                }
            }
        }
        private class AssetMethodUsagesData
        {
            public string unityEventName; 
            public string methodName;
            public EventHandlerArgumentMode mode = EventHandlerArgumentMode.EventDefined;
            public string type;
            public IHierarchyReference targetReference;
            public TextRange textRangeOwnerPsiPersistentIndex = TextRange.InvalidRange;
            public OWORD textRangeOwner;

            public AssetMethodUsages ToAssetMethodUsages()
            {
                return new AssetMethodUsages(unityEventName, methodName, textRangeOwnerPsiPersistentIndex,
                    textRangeOwner, mode, type, targetReference);
            }
        }
    }
}