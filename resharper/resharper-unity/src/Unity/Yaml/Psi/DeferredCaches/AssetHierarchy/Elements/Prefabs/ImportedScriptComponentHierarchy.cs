using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs
{
    public class ImportedScriptComponentHierarchy : IScriptComponentHierarchy
    {
        private readonly IPrefabInstanceHierarchy myPrefabInstanceHierarchy;
        private readonly IScriptComponentHierarchy myScriptComponentHierarchy;

        public ImportedScriptComponentHierarchy(IPrefabInstanceHierarchy prefabInstanceHierarchy,
            IScriptComponentHierarchy scriptComponentHierarchy)
        {
            myPrefabInstanceHierarchy = prefabInstanceHierarchy;
            myScriptComponentHierarchy = scriptComponentHierarchy;
        }

        public LocalReference Location => myScriptComponentHierarchy.Location.GetImportedReference(myPrefabInstanceHierarchy);

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => myScriptComponentHierarchy.Name;

        public LocalReference OwningGameObject => myScriptComponentHierarchy.OwningGameObject.GetImportedReference(myPrefabInstanceHierarchy);

        public ExternalReference ScriptReference => myScriptComponentHierarchy.ScriptReference;
        public List<Dictionary<string, IAssetValue>> ImportUnityEventData(UnityEventsElementContainer elementContainer, JetHashSet<string> allUnityEventNames)
        {
            var result = myScriptComponentHierarchy.ImportUnityEventData(elementContainer, allUnityEventNames);
            var patchData = GetImportedValuesForUnityEvent(Location, allUnityEventNames);

            var maxValue = patchData.Keys.Count == 0 ? 0 : patchData.Keys.Max();
            while (maxValue >= result.Count)
            {
                result.Add(new Dictionary<string, IAssetValue>());
            }
            
            foreach (var (i, patch) in patchData)
            {
                AssetUtils.Import(result[i], patch);
            }


            return result;
        }
        public Dictionary<int, Dictionary<string, IAssetValue>> GetImportedValuesForUnityEvent(LocalReference scriptLocation, JetHashSet<string> allUnityEventNames)
        {
            var result = new Dictionary<int, Dictionary<string, IAssetValue>>();
            foreach (var modification in myPrefabInstanceHierarchy.PrefabModifications)
            {
                if (!(modification.Target is ExternalReference externalReference))
                    continue;
                
                if (!modification.PropertyPath.Contains(".m_PersistentCalls."))
                    continue;
                
                var location = new LocalReference(Location.OwningPsiPersistentIndex, PrefabsUtil.GetImportedDocumentAnchor(myPrefabInstanceHierarchy.Location.LocalDocumentAnchor, externalReference.LocalDocumentAnchor));
                if (!location.Equals(scriptLocation))
                    continue;

                var (unityEventName, parts) = UnityEventUtils.SplitPropertyPath(modification.PropertyPath);
                if (!allUnityEventNames.Contains(unityEventName))
                    continue;

                if (!UnityEventUtils.TryGetDataIndex(parts, out var index))
                    continue;
                
                var last = parts.Last();
                if (!result.TryGetValue(index, out var modifications))
                {
                    modifications = new Dictionary<string, IAssetValue>();
                    result[index] = modifications;
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

            return result;
        }
    }
}