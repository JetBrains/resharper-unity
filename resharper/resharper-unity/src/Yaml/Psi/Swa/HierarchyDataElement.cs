using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    [PolymorphicMarshaller(1)]
    public class HierarchyDataElement : ISwaExtensionData, ISwaExtensionInfo
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly UnityVersion myUnityVersion;

        public readonly OneToCompactCountingSet<FileID, IUnityHierarchyElement> Elements =
            new OneToCompactCountingSet<FileID, IUnityHierarchyElement>();

        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as HierarchyDataElement);

        public HierarchyDataElement(MetaFileGuidCache metaFileGuidCache, UnityVersion unityVersion)
        {
            myMetaFileGuidCache = metaFileGuidCache;
            myUnityVersion = unityVersion;
        }

        public static void Write(UnsafeWriter writer, HierarchyDataElement dataElement)
        {
            writer.Write(dataElement.Elements.Count);

            foreach (var (id, elements) in dataElement.Elements)
            {
                id.WriteTo(writer);
                writer.Write(elements.Count);
                foreach (var (element, count) in elements)
                {
                    writer.WritePolymorphic(element);
                    writer.Write(count);
                }
            }
        }

        public static HierarchyDataElement Read(UnsafeReader reader)
        {
            var element = new HierarchyDataElement(null, null); // we do not need them more
            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var id = FileID.ReadFrom(reader);
                var elementsCounts = reader.ReadInt32();
                for (int j = 0; j < elementsCounts; j++)
                {
                    element.Elements.Add(id, reader.ReadPolymorphic<IUnityHierarchyElement>(), reader.ReadInt32());
                }
            }

            return element;
        }


        public void AddData(ISwaExtensionData data)
        {
            var otherHierarchyElement = (data as HierarchyDataElement).NotNull("otherHierarchyElement != null");
            foreach (var (id, elements) in otherHierarchyElement.Elements)
            {
                foreach (var (element, count) in elements)
                {
                    Elements.Add(id, element, count);
                }
            }
        }

        public ISwaExtensionInfo ToInfo(CollectUsagesStagePersistentData persistentData)
        {
            return this;
        }

        public void ProcessBeforeInterior(ITreeNode element, IParameters parameters)
        {
            if (element is IYamlDocument document)
            {
                var guid = myMetaFileGuidCache.GetAssetGuid(element.GetSourceFile());
                var tag = GetUnityObjectTag(document);
                var isStripped = IsStripped(document);
                var id = document.GetAnchor();
                if (guid == null || tag == null || id == null)
                    return;
                var objectId = new FileID(guid, id);
                
                if (tag.Equals("!u!1"))
                {
                    // gameObject
                    var name = document.GetUnityObjectPropertyValue(UnityYamlConstants.NameProperty).GetPlainScalarText() ?? string.Empty;
                    
                    var correspondingSourceObject = GetCorrespondingSourceObjectFileId(document);
                    var prefabInstance = GetPrefabInstanceFileId(document);
                    var components = document.GetUnityObjectPropertyValue(UnityYamlConstants.Components);
                    var componentsId = new List<FileID>();
                    if (components is IBlockSequenceNode componentSequence)
                    {
                        foreach (var entry in componentSequence.Entries)
                        {
                            var componentNode = entry.Value as IBlockMappingNode;
                            var componentFileID = componentNode?.EntriesEnumerable.FirstOrDefault()?.Content.Value
                                .AsFileID();
                            if (componentFileID == null)
                                continue;

                            if (!componentFileID.IsExternal)
                            {
                                componentsId.Add(componentFileID.WithGuid(guid));
                            }
                            else
                            {
                                componentsId.Add(componentFileID);
                            }
                        }
                    }

                    Elements.Add(objectId, 
                        new GameObjectHierarchyElement(objectId, correspondingSourceObject, prefabInstance,
                            isStripped, name, componentsId));
                }
                else if (tag.Equals("!u!4") || tag.Equals("!u!224"))
                {
                    var fatherId = document.GetUnityObjectPropertyValue(UnityYamlConstants.FatherProperty).AsFileID();
                    if (fatherId == null)
                        return;
                    
                    if (!fatherId.IsExternal)
                    {
                        fatherId = fatherId.WithGuid(guid);
                    }
                    
                    if (!int.TryParse(document.GetUnityObjectPropertyValue(UnityYamlConstants.RootOrderProperty).GetPlainScalarText(), out var rootOrder))
                        return;
                        
                    var gameObjectId = document.GetUnityObjectPropertyValue(UnityYamlConstants.GameObjectProperty).AsFileID();
                    if (gameObjectId == null)
                        return;
                    
                    if (!gameObjectId.IsExternal)
                    {
                        gameObjectId = gameObjectId.WithGuid(guid);
                    }
                    
                    var correspondingSourceObject = GetCorrespondingSourceObjectFileId(document);
                    var prefabInstance = GetPrefabInstanceFileId(document);
                    var children = document.GetUnityObjectPropertyValue(UnityYamlConstants.Children);
                    var childrenId = new List<FileID>();
                    if (children is IBlockSequenceNode componentSequence)
                    {
                        foreach (var entry in componentSequence.Entries)
                        {
                            var childId = entry.Value.AsFileID();
                            if (childId == null)
                                continue;

                            if (!childId.IsExternal)
                            {
                                childrenId.Add(childId.WithGuid(guid));
                            }
                            else
                            {
                                childrenId.Add(childId);
                            }
                        }
                    }

                    Elements.Add(objectId, 
                        new TransformHierarchyElement(objectId, correspondingSourceObject, prefabInstance,
                            isStripped, rootOrder, gameObjectId,  fatherId, childrenId));
                }
                else if (tag.Equals("!u!1001"))
                {
                    var modificationRoot = UnityObjectPsiUtil.GetPrefabModification(document);
                    var transformParent = modificationRoot.GetValue(UnityYamlConstants.TransformParentProperty).AsFileID();
                    var modifications = modificationRoot.GetValue(UnityYamlConstants.ModificationsProperty) as IBlockSequenceNode;

                    var indexes = new Dictionary<FileID, int?>();
                    var names = new Dictionary<FileID, string>();
                    if (modifications != null)
                    {
                        foreach (var modification in modifications.Entries)
                        {
                            var modNode = modification.Value as IBlockMappingNode;

                            var path = modNode.GetValue(UnityYamlConstants.PropertyPathProperty).GetPlainScalarText();
                            if (UnityYamlConstants.RootOrderProperty.Equals(path))
                            {
                                var target = modNode.GetValue(UnityYamlConstants.TargetProperty).AsFileID()?.WithGuid(null);
                                if (target == null)
                                    continue;

                                indexes[target] =int.TryParse(modNode.GetValue(UnityYamlConstants.ValueProperty).GetPlainScalarText(), out var index)
                                        ? (int?)index
                                        : null;
                                
                                
                            } else if (UnityYamlConstants.NameProperty.Equals(path))
                            {
                                var target = modNode.GetValue(UnityYamlConstants.TargetProperty).AsFileID()?.WithGuid(null);
                                if (target == null)
                                    continue;

                                names[target] = modNode.GetValue(UnityYamlConstants.ValueProperty).GetPlainScalarText();
                            }
                        }
                    }

                    Elements.Add(objectId,
                        new ModificationHierarchyElement(objectId, null, null, isStripped, transformParent, indexes,
                            names));
                }
                else
                {
                    var gameObjectId = document.GetUnityObjectPropertyValue(UnityYamlConstants.GameObjectProperty).AsFileID();
                    if (gameObjectId == null)
                        return;
                    
                    if (!gameObjectId.IsExternal)
                    {
                        gameObjectId = gameObjectId.WithGuid(guid);
                    }
                    
                    var correspondingSourceObject = GetCorrespondingSourceObjectFileId(document);
                    var prefabInstance = GetPrefabInstanceFileId(document);

                    Elements.Add(objectId, 
                        new ComponentHierarchyElement(objectId, correspondingSourceObject, prefabInstance,
                            gameObjectId, isStripped));
                }
            }
        }

        private static bool IsStripped(IYamlDocument element)
        {
            return ((element.Body.BlockNode as IBlockMappingNode)?.Properties?.LastChild as
                       YamlTokenType.GenericTokenElement)?
                   .GetText().Equals("stripped") == true;
        }

        public static string GetUnityObjectTag(IYamlDocument document)
        {
            var tag = (document.Body.BlockNode as IBlockMappingNode)?.Properties.TagProperty.GetText();
            return tag;
        }

        private FileID GetCorrespondingSourceObjectFileId(IYamlDocument document)
        {
            if (myUnityVersion.GetActualVersionForSolution().Major == 2017)
                return document.GetUnityObjectPropertyValue(UnityYamlConstants.CorrespondingSourceObjectProperty2017)
                    ?.AsFileID();

            return document.GetUnityObjectPropertyValue(UnityYamlConstants.CorrespondingSourceObjectProperty)
                       ?.AsFileID() ??
                   document.GetUnityObjectPropertyValue(UnityYamlConstants.CorrespondingSourceObjectProperty2017)
                       ?.AsFileID();
        }

        private FileID GetPrefabInstanceFileId(IYamlDocument document)
        {
            if (myUnityVersion.GetActualVersionForSolution().Major == 2017)
                return document.GetUnityObjectPropertyValue(UnityYamlConstants.PrefabInstanceProperty2017)?.AsFileID();

            return document.GetUnityObjectPropertyValue(UnityYamlConstants.PrefabInstanceProperty)?.AsFileID() ??
                   document.GetUnityObjectPropertyValue(UnityYamlConstants.PrefabInstanceProperty2017)?.AsFileID();
        }

        public void ProcessAfterInterior(ITreeNode element, IParameters parameters)
        {
        }

        public void ProcessNode(ITreeNode element, IParameters parameters)
        {
        }
    }
}