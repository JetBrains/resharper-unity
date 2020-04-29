using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Extension;

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
        /// 3. GetPossibleFilesWithUsage will check that assosiated with guid type element is derived from method's type element and process only in that case
        /// NB : resolve to real ScriptComponentHierarchy will be cached in PrefabImportCache for stripped elements or will be simply availble in current scene hierarchy
        /// </summary>
        private readonly CountingSet<IPsiSourceFile> myFilesToCheckForUsages = new CountingSet<IPsiSourceFile>();
        
        // both collection could be removed and replaced by pointer to UnityEventsDataElement with coressponding sourceFile
        // NB: LocalReference == pointer to source file too.
        private readonly Dictionary<IPsiSourceFile, ImportedUnityEventData> myImportedUnityEventDatas = new Dictionary<IPsiSourceFile, ImportedUnityEventData>();
        #endregion
        
        
        private ImportedUnityEventData ProcessPrefabModifications(IPsiSourceFile currentFile, AssetDocument document)
        {
            var result = new ImportedUnityEventData();
            if (document.HierarchyElement is IPrefabInstanceHierarchy prefabInstanceHierarchy)
            {
                foreach (var modification in prefabInstanceHierarchy.PrefabModifications)
                {

                    if (!(modification.Target is ExternalReference externalReference))
                        continue;
                    
                    if (!modification.PropertyPath.Contains("m_PersistentCalls"))
                        continue;
                    
                    var location = new LocalReference(currentFile.PsiStorage.PersistentIndex, PrefabsUtil.Import(prefabInstanceHierarchy.Location.LocalDocumentAnchor, externalReference.LocalDocumentAnchor));
                    var parts = modification.PropertyPath.Split('.');
                    var unityEventName = parts[0];

                    var dataPart = parts.FirstOrDefault(t => t.StartsWith("data"));
                    if (dataPart == null)
                        continue;
                    
                    if (!int.TryParse(dataPart.RemoveStart("data[").RemoveEnd("]"), out var index))
                        continue;

                    var last = parts.Last();


                    var reference = new ImportedAssetMethodReference(location, unityEventName, index);
                    if (!result.ReferenceToImportedData.TryGetValue(reference, out var modifications))
                    {
                        modifications = new Dictionary<string, IAssetValue>();
                        result.ReferenceToImportedData[reference] = modifications;
                    }

                    switch (last)
                    {
                        case "m_Mode" when modification.Value is AssetSimpleValue simpleValue:
                            modifications[last] = simpleValue;
                            break;
                        case "m_MethodName" when modification.Value is AssetSimpleValue simpleValue:
                            modifications[last] = simpleValue;
                            modifications["m_MethodNameRange"] = new Int2Value(modification.ValueRange.StartOffset, modification.ValueRange.EndOffset);
                            break;
                        case "m_Target" when modification.ObjectReference is IHierarchyReference objectReference:
                            modifications[last] = new AssetReferenceValue(objectReference);
                            break;
                    }
                }
            }
        
            return result;
        }

        private void DropPrefabModifications(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, UnityEventsDataElement element)
        {
            foreach (var (reference, modification) in element.ImportedUnityEventData.ReferenceToImportedData)
            {
                if (modification.ContainsKey("m_Target") && !modification.ContainsKey("m_MethodName"))
                    myFilesToCheckForUsages.Remove(sourceFile);

                if (modification.TryGetValue("m_MethodName", out var name))
                    myMethodNameToFilesWithPossibleUsages.Remove((name as AssetSimpleValue).NotNull("name as AssetSimpleValue != null").SimpleValue, sourceFile);

                myUnityEventsWithModifications.Remove(reference.UnityEventName);
                myUnityEventNameToSourceFiles.Remove(reference.UnityEventName, sourceFile);

            }
            myImportedUnityEventDatas.Remove(sourceFile);
        }

        private void MergePrefabModifications(IPsiSourceFile sourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, UnityEventsDataElement element)
        {
            foreach (var (reference, modification) in element.ImportedUnityEventData.ReferenceToImportedData)
            {
                if (!modification.ContainsKey("m_MethodName"))
                    myFilesToCheckForUsages.Add(sourceFile);
                
                if (modification.TryGetValue("m_MethodName", out var name))
                    myMethodNameToFilesWithPossibleUsages.Add((name as AssetSimpleValue).NotNull("name as AssetSimpleValue != null").SimpleValue, sourceFile);

                myUnityEventsWithModifications.Add(reference.UnityEventName);
                myUnityEventNameToSourceFiles.Add(reference.UnityEventName, sourceFile);
            }
            myImportedUnityEventDatas[sourceFile] = element.ImportedUnityEventData;
        }

        private IEnumerable<AssetMethodData> GetImportedAssetMethodDataFor(IPsiSourceFile psiSourceFile)
        {
            if (myImportedUnityEventDatas.TryGetValue(psiSourceFile, out var importedUnityEventData))
            {
                foreach (var (reference, modifications) in importedUnityEventData.ReferenceToImportedData)
                {
                    var element = myAssetDocumentHierarchyElementContainer.GetHierarchyElement(reference.Location, true);
                    Assertion.Assert(element is ImportedScriptComponentHierarchy, "element is ImportedScriptComponentHierarchy");
                    var script = element as ImportedScriptComponentHierarchy;
                    var originLocation = script.OriginLocation;



                    if (!myUnityEventDatas.TryGetValue((originLocation, reference.UnityEventName), out var unityEventData))
                    {
                        // TODO : check FormerlySerializedAs
                        // 1. get script type from script.ScriptReference.ExternalAssetGuid
                        // 2. find field which could be related to reference.UnityEventName (same short name, or same formerlyName)
                        // 3. extr
                        var scriptType = AssetUtils.GetTypeElementFromScriptAssetGuid(mySolution, script.ScriptReference.ExternalAssetGuid);

                        var field = scriptType?.GetMembers().FirstOrDefault(t => t is IField f && AssetUtils.GetAllNamesFor(f).Contains(reference.UnityEventName)) as IField;
                        if (field == null)
                            continue;

                        var names = AssetUtils.GetAllNamesFor(field);
                        
                        // TODO how we should merge entries if several names stored in script component?, let's take first one for now

                        foreach (var name in names)
                        {
                            if (myUnityEventDatas.TryGetValue((originLocation, name), out unityEventData))
                                break;
                        }
                            
                        if (unityEventData == null)
                            continue;
                    }

                    if (reference.MethodIndex < unityEventData.Calls.Count)
                    {
                        var import = unityEventData.Calls[reference.MethodIndex].Import(reference, modifications);
                        if (import != null)
                            yield return import;
                    }
                    else
                    {
                        var result = AssetMethodData.TryCreateAssetMethodFromModifications(reference, modifications);
                        if (result != null)
                            yield return result;
                    }
                }
            }
        }
    }
}