using System;
using System.Collections.Generic;
using JetBrains.Application;
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
        // We need to be able to check if a method is declared on a base type but used in a deriving type. We keep a map
        // of method/property setter short name to all the asset guids where it's used. These usages will always be the
        // most derived type. If we get all (inherited) members of each usage, we can match to see if a given method
        // (potentially declared on a base type) is being used as an event handler
        public readonly OneToSetMap<string, string> ShortNameToAssetGuid;
        public readonly SceneHierarchy SceneHierarchy;

        public static readonly IUnsafeMarshaller<UnitySceneData> Marshaller = new UniversalMarshaller<UnitySceneData>(Read, Write);
        
        private UnitySceneData(OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> propertiesData,OneToSetMap<string, string> eventHandlers, SceneHierarchy sceneHierarchy)
        {
            PropertiesData = propertiesData;
            SceneHierarchy = sceneHierarchy;
            ShortNameToAssetGuid = eventHandlers;
        }
        
        private static UnitySceneData Read(UnsafeReader reader)
        {
            return new UnitySceneData(PropertiesDataMarshaller.Unmarshal(reader), EventHandlersMarshaller.Unmarshal(reader), SceneHierarchy.Read(reader));
        }

        private static void Write(UnsafeWriter writer, UnitySceneData value)
        {
            PropertiesDataMarshaller.Marshal(writer, value.PropertiesData);
            EventHandlersMarshaller.Marshal(writer, value.ShortNameToAssetGuid);
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

        private static IUnsafeMarshaller<OneToSetMap<string, string>> EventHandlersMarshaller = 
            UnsafeMarshallers
                .GetOneToManyMapMarshaller<string, string, ISet<string>, OneToSetMap<string, string>>
                (
                    UnsafeMarshallers.UnicodeStringMarshaller,
                    UnsafeMarshallers.UnicodeStringMarshaller,
                    n => new OneToSetMap<string, string>(n)
                );
        
        public static UnitySceneData Build(IUnityYamlFile file)
        {
            Assertion.Assert(file.IsValid(), "file.IsValid()");
            Assertion.Assert(file.GetSolution().Locks.IsReadAccessAllowed(), "ReadLock is required");
            
            var interruptChecker = new SeldomInterruptChecker();
            var unityPropertyValueCacheItem = new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>();
            var sceneHierarchy = new SceneHierarchy();
            
            var anchorToGuid = new Dictionary<string, string>();
            var anchorToEventHandler = new OneToSetMap<string, string>();
            var eventHandlerToGuid = new OneToSetMap<string, string>();
            
            foreach (var document in file.DocumentsEnumerable)
            {
                interruptChecker.CheckForInterrupt();
                var buffer = document.GetTextAsBuffer();
                if (ourPrefabModificationSearcher.Find(buffer, 0, Math.Min(buffer.Length, 100)) > 0)
                {
                    sceneHierarchy.AddPrefabModification(buffer);
                }
                else
                {
                    var simpleValues = new Dictionary<string, string>();
                    var referenceValues = new Dictionary<string, FileID>();
                    var targetToMethods = new OneToSetMap<string, string>();
                    UnitySceneDataUtil.ExtractSimpleAndReferenceValues(buffer, simpleValues, referenceValues, targetToMethods);

                    FillProperties(simpleValues, referenceValues, unityPropertyValueCacheItem);

                    sceneHierarchy.AddSceneHierarchyElement(simpleValues, referenceValues);

                    var anchor = simpleValues.GetValueSafe("&anchor");
                    var fileId = referenceValues.GetValueSafe(UnityYamlConstants.ScriptProperty);
                    if (anchor != null && fileId != null)
                        anchorToGuid[anchor] = fileId.guid;

                    foreach (var key in targetToMethods.Keys)
                    {
                        anchorToEventHandler.AddRange(key, targetToMethods[key]);
                    }
                    
                }
            }

            foreach (var (anchor, shortNames) in anchorToEventHandler)
            {
                var guid = anchorToGuid.GetValueSafe(anchor);
                if (guid == null)
                    continue;
                foreach (var shortName in shortNames)
                {
                    eventHandlerToGuid.Add(shortName, guid);
                }
            }
            
            if (unityPropertyValueCacheItem.Count == 0 && sceneHierarchy.Elements.Count == 0)
                return null;

            return new UnitySceneData(unityPropertyValueCacheItem, eventHandlerToGuid, sceneHierarchy);
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