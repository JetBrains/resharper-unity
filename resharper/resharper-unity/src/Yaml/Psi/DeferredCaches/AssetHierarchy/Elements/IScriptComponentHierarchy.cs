using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    public interface IScriptComponentHierarchy : IComponentHierarchy
    {
        ExternalReference ScriptReference { get; }
        
        List<Dictionary<string, IAssetValue>> ImportUnityEventData(UnityEventsElementContainer elementContainer, JetHashSet<string> allUnityEventNames);
    }
}