using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Deserializers;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues
{
    [SolutionComponent]
    public class AssetInspectorValuesContainer : IUnityAssetDataElementContainer
    {
        private readonly ILogger myLogger;
        private readonly List<IAssetInspectorValueDeserializer> myDeserializers;
        public AssetInspectorValuesContainer(IEnumerable<IAssetInspectorValueDeserializer> assetInspectorValueDeserializer, ILogger logger)
        {
            myLogger = logger;
            myDeserializers = assetInspectorValueDeserializer.OrderByDescending(t => t.Order).ToList();
        }
        
        public IUnityAssetDataElement Build(Lifetime lifetime, IPsiSourceFile currentSourceFile, AssetDocument assetDocument)
        {
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {
                var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(assetDocument.Buffer);
                if (anchor == null)
                    return null;
                
                var dictionary = new Dictionary<string, IAssetValue>();
                var entries = assetDocument.Document.FindRootBlockMapEntries()?.Entries;
                foreach (var entry in entries)
                {
                    var key = entry.Key.GetPlainScalarText();
                    if (key == null || ourIgnoredMonoBehaviourEntries.Contains(key))
                        continue;

                    foreach (var deserializer in myDeserializers)
                    {
                        try
                        {
                            if (deserializer.TryGetInspectorValue(currentSourceFile, entry.Content, out var result))
                            {
                                dictionary[key] = result;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            myLogger.Error(e, "An error occurred while deserializing value {0}", deserializer.GetType().Name);
                        }
                    }
                }

                if (dictionary.TryGetValue(UnityYamlConstants.ScriptProperty, out var scriptValue) && scriptValue is AssetReferenceValue referenceValue)
                {
                    var location = new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, anchor);
                    var script = referenceValue.Reference;
                    foreach (var (key, value) in dictionary)
                    {
                        var usage = new InspectorVariableUsage(location, script, key, value);
                    }
                }
            }

            return null;
        }

        public void Drop(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            throw new System.NotImplementedException();
        }

        public void Merge(IPsiSourceFile sourceFile, IUnityAssetDataElement unityAssetDataElement)
        {
            throw new System.NotImplementedException();
        }
        
        private static readonly HashSet<string> ourIgnoredMonoBehaviourEntries = new HashSet<string>()
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_PrefabAsset",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_EditorClassIdentifier"
        };
        
        public string Id => nameof(AssetInspectorValuesContainer);
    }
}