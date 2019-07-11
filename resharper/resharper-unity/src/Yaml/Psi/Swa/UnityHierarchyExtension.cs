using System.Linq;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    [SolutionComponent]
    public class UnityHierarchyExtension : SwaExtensionProviderBase
    {
        private readonly object myLockObject = new object();
        
        private readonly MetaFileGuidCache myCache;
        private readonly UnityVersion myUnityVersion;

        public readonly OneToCompactCountingSet<FileID, IUnityHierarchyElement> Elements =
            new OneToCompactCountingSet<FileID, IUnityHierarchyElement>();
        
        public UnityHierarchyExtension(UnitySolutionTracker solutionTracker, MetaFileGuidCache cache, UnityVersion unityVersion) 
            : base("UnityHierarchyInfo", solutionTracker.IsUnityProject.HasTrueValue())
        {
            myCache = cache;
            myUnityVersion = unityVersion;
        }

        public override ISwaExtensionData CreateUsageDataElement(UsageData owner)
        {
            return new HierarchyDataElement(myCache, myUnityVersion);
        }

        public override void Merge(ISwaExtensionInfo old, ISwaExtensionInfo @new)
        {
            var oldData = old as HierarchyDataElement;
            var newData = @new as HierarchyDataElement;

            lock (myLockObject)
            {
                if (oldData != null)
                {
                    foreach (var (fileId, elements) in oldData.Elements)
                    {
                        foreach (var (element, count) in elements)
                        {
                            Elements.Remove(fileId, element, count);
                        }
                    }
                }

                if (newData != null)
                {
                    foreach (var (fileId, elements) in newData.Elements)
                    {
                        foreach (var (element, count) in elements)
                        {
                            Elements.Add(fileId, element, count);
                        }
                    }
                }
            }
        }

        public override void Clear()
        {
            lock (myLockObject)
            {
                Elements.Clear();
            }
        }

        public void ProcessSceneHierarchyFromComponentToRoot(IYamlDocument startComponent, IUnityCachedSceneProcessorConsumer consumer)
        {
            var guid = myCache.GetAssetGuid(startComponent.GetSourceFile());
            var anchor = startComponent.GetAnchor();
            if (guid == null || anchor == null)
                return;
            
            ProcessSceneHierarchyFromComponentToRootUnderLock(new FileID(guid, anchor),  consumer);
        }


        private void ProcessSceneHierarchyFromComponentToRootUnderLock(FileID id, IUnityCachedSceneProcessorConsumer consumer)
        {
            lock (myLockObject)
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
        }
        
        public void ProcessSceneHierarchyFromComponentToRoot(FileID id, IUnityCachedSceneProcessorConsumer consumer)
        {
            ProcessSceneHierarchyFromComponentToRootUnderLock(id, consumer);
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

    }
}