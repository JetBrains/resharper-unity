using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public readonly struct ScriptComponentHierarchy : IScriptComponentHierarchy
    {
        public LocalReference Location { get; }
        public LocalReference OwningGameObject { get; }
        public ExternalReference ScriptReference { get; }

        public ScriptComponentHierarchy(LocalReference location, LocalReference owner, ExternalReference scriptReference)
        {
            Location = location;
            OwningGameObject = owner;
            ScriptReference = scriptReference;
        }

        public IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public string Name => "MonoBehaviour";

        public static void Write(UnsafeWriter writer, ScriptComponentHierarchy scriptComponentHierarchy)
        {
            scriptComponentHierarchy.Location.WriteTo(writer);
            scriptComponentHierarchy.OwningGameObject.WriteTo(writer);
            scriptComponentHierarchy.ScriptReference.WriteTo(writer);
        }

        public static ScriptComponentHierarchy Read(UnsafeReader reader)
        {
            return new ScriptComponentHierarchy(
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
                HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
        
        public List<Dictionary<string, IAssetValue>> ImportUnityEventData(UnityEventsElementContainer elementContainer, JetHashSet<string> allUnityEventNames)
        {
            var unityEvents = elementContainer.GetUnityEventDataFor(Location, allUnityEventNames);

            var result = new List<Dictionary<string, IAssetValue>>();
            foreach (var unityEventData in unityEvents)
            {
                for (int i = 0; i < unityEventData.Calls.Count; i++)
                {
                    var dictionary = unityEventData.Calls[i].ToDictionary();
                    if (i < result.Count)
                    {
                        AssetUtils.Import(result[i], dictionary);
                    }
                    else
                    {
                        Assertion.Assert(result.Count == i, "result.Count == i");
                        result.Add(dictionary);
                    }
                }    
            }
            return result;
        }
    }
}