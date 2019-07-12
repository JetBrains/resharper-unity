using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    [SolutionComponent]
    public class UnityHierarchyExtension : SimpleICache<HierarchyDataElement>
    {
        private readonly MetaFileGuidCache myCache;
        private readonly UnityVersion myUnityVersion;

        public readonly OneToCompactCountingSet<FileID, IUnityHierarchyElement> Elements =
            new OneToCompactCountingSet<FileID, IUnityHierarchyElement>();


        public UnityHierarchyExtension(Lifetime lifetime, UnitySolutionTracker solutionTracker, MetaFileGuidCache cache,
            UnityVersion unityVersion, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, new UniversalMarshaller<HierarchyDataElement>(HierarchyDataElement.ReadDelegate, HierarchyDataElement.WriteDelegate))
        {
            myCache = cache;
            myUnityVersion = unityVersion;
        }
        
        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<UnityYamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }
        
        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            var data = new HierarchyDataElement(myCache, myUnityVersion);
            data.Build(sourceFile);
            return data;
        }


        public void ProcessSceneHierarchyFromComponentToRoot(IYamlDocument startComponent, IUnityCachedSceneProcessorConsumer consumer)
        {
            var guid = myCache.GetAssetGuid(startComponent.GetSourceFile());
            var anchor = startComponent.GetAnchor();
            if (guid == null || anchor == null)
                return;
            
            ProcessSceneHierarchyFromComponentToRoot(new FileID(guid, anchor),  consumer);
        }


        private void ProcessSceneHierarchyFromComponentToRoot(FileID id, IUnityCachedSceneProcessorConsumer consumer)
        {
            var start = Elements.GetValues(id).FirstOrDefault();
            if (start == null)
                return;
            if (!start.IsStripped) // start component can be stripped, e.g : prefab's MonoBehavior's function is passed to button event handler
            {

                FileID gameObjectId;
                if (start is TransformHierarchyElement transformHierarchyElement)
                {
                    gameObjectId = transformHierarchyElement.GameObject;
                } else if (start is ComponentHierarchyElement componentHierarchyElement)
                {
                    gameObjectId = componentHierarchyElement.GameObject;
                }
                else
                {
                    Assertion.Fail("Unexpected hierarchy element");
                    return;
                }
                
                // Component must be attached to game object
                var gameObject = Elements.GetValues(gameObjectId).FirstOrDefault() as GameObjectHierarchyElement;
                if (gameObject == null)
                    return;
            
                // GameObject could be stripped, if another prefab's gameobject is modified via adding MonoBehaviour
                if (!gameObject.IsStripped)
                {
                    // Each GameObject must have Transform. We will use it to process scene hierarcy
                    ProcessSceneHierarchyFromComponentToRootInner(GetTransformComponent(gameObject), consumer, null);  
                } else 
                {
                    ProcessSceneHierarchyFromComponentToRootInner(gameObject, consumer, null);
                }
            }
            else
            {
                ProcessSceneHierarchyFromComponentToRootInner(start, consumer, null);
            }
        }
        
        
        // Invariant : startGameObject is Transform Component if it is not stripped 
        // This method traverse scene hierarchy via visiting transform components and push corresponding to transform GameObject into consumer
        private void ProcessSceneHierarchyFromComponentToRootInner(IUnityHierarchyElement startUnityObject, IUnityCachedSceneProcessorConsumer consumer, ModificationHierarchyElement modifications)
        {
            
            var currentUnityObject = startUnityObject;
            while (currentUnityObject != null)
            {
                // Unity object could be stripped, it means, that corresponding real object belongs to another yaml file
                // Also, it has reference to prefab instance in current file, which stores all prefab modification
                if (currentUnityObject.IsStripped)
                {
                    var correspondingId = currentUnityObject.CorrespondingSourceObject;
                    var prefabInstanceId = currentUnityObject.PrefabInstance;
                    
                    // assert not null
                    if (correspondingId == null || prefabInstanceId == null)
                        return;

                    var prefabInstance = Elements.GetValues(prefabInstanceId).FirstOrDefault() as ModificationHierarchyElement;

                    var prefabStartGameObject = Elements.GetValues(correspondingId).FirstOrDefault();
                    
                    if (prefabStartGameObject == null)
                        return; // TODO [vkrasnotsvetov] 19.3 Handle case, when prefab contains prefab which contains prefab
                    
                    if (!prefabStartGameObject.IsStripped)
                    {
                        // !u!4 is transform. If tag is different, let's extract transform, there are two cases:
                        // 1) prefabStartGameObject is GameObject(!u!1), take its transform
                        // 2) prefabStartGameObject is Component, so get attached gameobject and from this gameobject take transform component
                        if (!(prefabStartGameObject is TransformHierarchyElement))
                        {
                            var attachedGameObject= prefabStartGameObject;
                            if (attachedGameObject is ComponentHierarchyElement componentHierarchyElement)
                            {
                                attachedGameObject = Elements.GetValues(componentHierarchyElement.GameObject).FirstOrDefault();
                            }

                            if (attachedGameObject is GameObjectHierarchyElement gameObjectHierarchyElement)
                            {
                                prefabStartGameObject = GetTransformComponent(gameObjectHierarchyElement);
                            }
                            else
                            {
                                prefabStartGameObject = null;
                            }
                        }
                    }
                    ProcessSceneHierarchyFromComponentToRootInner(prefabStartGameObject, consumer, prefabInstance);
                    currentUnityObject = prefabInstance != null ? Elements.GetValues(prefabInstance.TransformParentId).FirstOrDefault() : null;
                }
                else
                {
                    // assert that startGameObject is GameObject
                    var transformComponent = currentUnityObject as TransformHierarchyElement;
                    if (transformComponent == null)
                        return; // strange, log here
                    
                    Assertion.Assert(currentUnityObject is TransformHierarchyElement, "currentUnityObject is TransformHierarchyElement");



                    var father = Elements.GetValues(transformComponent.Father).FirstOrDefault();
                    var gameObject = Elements.GetValues(transformComponent.GameObject).FirstOrDefault() as GameObjectHierarchyElement;
                    if (gameObject == null)
                        return; // strange, log here
                    consumer.ConsumeGameObject(gameObject, transformComponent, modifications);
                    currentUnityObject = father;
                }
            }
        }

        private TransformHierarchyElement GetTransformComponent(GameObjectHierarchyElement gameObjectHierarchyElement)
        {
            foreach (var id in gameObjectHierarchyElement.Components)
            {
                var possibleTransform = Elements.GetValues(id).FirstOrDefault();
                if (possibleTransform is TransformHierarchyElement transform)
                {
                    return transform;
                }
            }

            return null;
        }
        
        
        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
            if (builtPart is HierarchyDataElement cache)
            {
                AddToLocalCache(sourceFile, cache);
            }
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (file, dataElement) in Map)
                AddToLocalCache(file, dataElement);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, HierarchyDataElement dataElement)
        {
            AddData(dataElement);
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var dataElement))
            {
                RemoveData(dataElement);
            }
        }
        
        public void AddData(HierarchyDataElement data)
        {
            foreach (var (id, elements) in data.Elements)
            {
                foreach (var (element, count) in elements)
                {
                    Elements.Add(id, element, count);
                }
            }
        }
        
        public void RemoveData(HierarchyDataElement data)
        {
            foreach (var (id, elements) in data.Elements)
            {
                foreach (var (element, count) in elements)
                {
                    Elements.Add(id, element, -count);
                }
            }
        }
    }
}