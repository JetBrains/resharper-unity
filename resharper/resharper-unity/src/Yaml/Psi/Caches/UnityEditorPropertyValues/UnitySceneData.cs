using System;
using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public class UnitySceneData
    {
        private static readonly StringSearcher ourPrefabModificationSearcher = new StringSearcher("!u!1001", true);
        
        public readonly OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> PropertiesData;
        public readonly SceneHierarchy SceneHierarchy;

        public static readonly IUnsafeMarshaller<UnitySceneData> Marshaller = new UniversalMarshaller<UnitySceneData>(Read, Write);
        
        private UnitySceneData(OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> propertiesData, SceneHierarchy sceneHierarchy)
        {
            PropertiesData = propertiesData;
            SceneHierarchy = sceneHierarchy;
        }
        
        private static UnitySceneData Read(UnsafeReader reader)
        {
            return new UnitySceneData(PropertiesDataMarshaller.Unmarshal(reader), SceneHierarchy.Read(reader));
        }

        private static void Write(UnsafeWriter writer, UnitySceneData value)
        {
            PropertiesDataMarshaller.Marshal(writer, value.PropertiesData);
            value.SceneHierarchy.WriteTo(writer);
        }
        
        
        private static IUnsafeMarshaller<OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>> PropertiesDataMarshaller = 
            UnsafeMarshallers
                .GetOneToManyMapMarshaller<MonoBehaviourProperty, MonoBehaviourPropertyValue,
                    IList<MonoBehaviourPropertyValue>, OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
                (
                    new UniversalMarshaller<MonoBehaviourProperty>(MonoBehaviourProperty.ReadFrom,
                        MonoBehaviourProperty.WriteTo),
                    new UniversalMarshaller<MonoBehaviourPropertyValue>(
                        MonoBehaviourPropertyValueMarshaller.Read,
                        MonoBehaviourPropertyValueMarshaller.Write),
                    n => new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>(n)
                );

        public static UnitySceneData Build(IUnityYamlFile file)
        {
            Assertion.Assert(file.IsValid(), "file.IsValid()");
            Assertion.Assert(file.GetSolution().Locks.IsReadAccessAllowed(), "ReadLock is required");
            
            
            var unityPropertyValueCacheItem = new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>();
            var sceneHierarchy = new SceneHierarchy();
            
            foreach (var document in file.DocumentsEnumerable)
            {
                var buffer = document.GetTextAsBuffer();
                if (ourPrefabModificationSearcher.Find(buffer, 0, Math.Min(buffer.Length, 100)) > 0)
                {
                    sceneHierarchy.AddPrefabModification(buffer);
                }
                else
                {
                    var simpleValues = new Dictionary<string, string>();
                    var referenceValues = new Dictionary<string, FileID>();
                    UnitySceneDataUtil.ExtractSimpleAndReferenceValues(buffer, simpleValues, referenceValues);

                    FillProperties(simpleValues, referenceValues, unityPropertyValueCacheItem);

                    sceneHierarchy.AddSceneHierarchyElement(simpleValues, referenceValues);
                }

            }

            
            
            if (unityPropertyValueCacheItem.Count == 0 && sceneHierarchy.Elements.Count == 0)
                return null;

            return new UnitySceneData(unityPropertyValueCacheItem, sceneHierarchy);
        }

        private static void FillProperties(Dictionary<string, string> simpleValues, Dictionary<string, FileID> referenceValues,
            OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> result)
        {
            var anchor = simpleValues.GetValueSafe("&anchor");
            if (anchor == null)
                return;

            var guid = referenceValues.GetValueSafe(UnityYamlConstants.ScriptProperty)?.guid;
            if (guid == null)
                return;

            var gameObject = referenceValues.GetValueSafe(UnityYamlConstants.GameObjectProperty)?.fileID;
            if (gameObject == null)
                return;

            foreach (var (fieldName, value) in simpleValues)
            {
                if (ourIgnoredMonoBehaviourEntries.Contains(fieldName))
                    continue;

                var property = new MonoBehaviourProperty(guid, fieldName);
                var propertyValue = new MonoBehaviourPrimitiveValue(value, anchor, gameObject);
                result.Add(property, propertyValue);
            }

            foreach (var (fieldName, value) in referenceValues)
            {
                if (ourIgnoredMonoBehaviourEntries.Contains(fieldName))
                    continue;

                var property = new MonoBehaviourProperty(guid, fieldName);
                var propertyValue = new MonoBehaviourReferenceValue(value, anchor, gameObject);
                result.Add(property, propertyValue);
            }
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
            "m_Script",
            UnityYamlConstants.NameProperty,
            "m_EditorClassIdentifier"
        };
    }
}