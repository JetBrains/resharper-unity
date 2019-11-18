using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [SolutionComponent]
    public class UnitySceneDataLocalCache
    {
        private readonly ISolution mySolution;
        private readonly MetaFileGuidCache myGuidCache;
        private readonly IShellLocks myShellLocks;

        public UnitySceneDataLocalCache(ISolution solution, MetaFileGuidCache guidCache, IShellLocks shellLocks)
        {
            mySolution = solution;
            myGuidCache = guidCache;
            myShellLocks = shellLocks;
        }

        private readonly PropertyValueLocalCache myPropertyValueLocalCache = new PropertyValueLocalCache();
        private readonly OneToCompactCountingSet<SceneElementId, IUnityHierarchyElement> mySceneElements
            = new OneToCompactCountingSet<SceneElementId, IUnityHierarchyElement>();
        
        private readonly OneToCompactCountingSet<string, (IPsiSourceFile sourceFile, FileID id)> myShortNameToScriptTarget = 
            new OneToCompactCountingSet<string, (IPsiSourceFile sourceFile, FileID id)>();

        private readonly Dictionary<IPsiSourceFile, OneToSetMap<string, string>> myScriptMapping =
            new Dictionary<IPsiSourceFile, OneToSetMap<string, string>>();

        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetPropertyValues(string guid, string propertyName)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myPropertyValueLocalCache.GetPropertyValues(query);
        }
        
        public int GetValueCount(string guid, string propertyName, object value)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            return myPropertyValueLocalCache.GetValueCount(new MonoBehaviourProperty(guid, propertyName), value);
        }

        public int GetPropertyValuesCount(string guid, string propertyName)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            return myPropertyValueLocalCache.GetPropertyValuesCount(new MonoBehaviourProperty(guid, propertyName));
        }
        
        public int GetPropertyUniqueValuesCount(string guid, string propertyName)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            return myPropertyValueLocalCache.GetPropertyUniqueValuesCount(new MonoBehaviourProperty(guid, propertyName));
        }
        
        public IEnumerable<object> GetUniqueValues(string guid, string propertyName)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myPropertyValueLocalCache.GetUniqueValues(query);
        }
        
        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetUniqueValuesWithLocation(string guid, string propertyName)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myPropertyValueLocalCache.GetUniqueValuesWithLocation(query);
        }
        
        public int GetFilesCountWithoutChanges(string guid, string propertyName, object value)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myPropertyValueLocalCache.GetFilesCountWithoutChanges(query, value);
        }

        public int GetFilesWithPropertyCount(string guid, string propertyName)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myPropertyValueLocalCache.GetFilesWithPropertyCount(query);
        }

        public void Add(IPsiSourceFile sourceFile, UnitySceneData sceneData)
        {
            myShellLocks.AssertMainThread();

            foreach (var (property, values) in sceneData.PropertiesData)
            {
                foreach (var value in values)
                {
                    myPropertyValueLocalCache.Add(property, new MonoBehaviourPropertyValueWithLocation(sourceFile, value));
                }
            }
            
            foreach (var (id, element) in sceneData.SceneHierarchy.Elements)
            {
                mySceneElements.Add(new SceneElementId(sourceFile, id), element);
            }

            foreach (var (name, ids) in sceneData.ShortNameToScriptFileId)
            {
                foreach (var id in ids)
                {
                    myShortNameToScriptTarget.Add(name, (sourceFile, id));
                }
            }
            
            myScriptMapping.Add(sourceFile, sceneData.ScriptMapping);
        }

        public bool IsEventHandler([NotNull] IMethod declaredElement)
        {
            var sourceFiles = declaredElement.GetSourceFiles();

            // The methods and property setters that we are interested in will only have a single source file
            if (sourceFiles.Count != 1)
                return false;

            foreach (var (sourceFile, scriptAnchor) in myShortNameToScriptTarget.GetValues(declaredElement.ShortName))
            {
                var scriptAnchorSourceFile = GetSourceFileWithPointedYamlDocument(sourceFile, scriptAnchor, myGuidCache);
                var guid = GetScriptGuid(scriptAnchorSourceFile, scriptAnchor.fileID);
                if (guid == null)
                    continue;
                
                var invokedType = UnityObjectPsiUtil.GetTypeElementFromScriptAssetGuid(mySolution, guid);
                if (invokedType != null)
                {
                    var members = invokedType.GetAllClassMembers(declaredElement.ShortName);
                    foreach (var member in members)
                    {
                        if (Equals(member.Element, declaredElement))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// FileId is reference to some object in Unity asset file. If guid property is defined in asset file, 
        /// FileId references some object in another Unity asset file which could be resovled by this guid ant MetaFileGuidCache
        /// </summary>
        public static IPsiSourceFile GetSourceFileWithPointedYamlDocument(IPsiSourceFile containingFile, FileID id, MetaFileGuidCache guidCache)
        {
            var module = (containingFile.PsiModule as UnityExternalFilesPsiModule).NotNull("module != null");
            var guid = id.guid;
            if (guid != null)
            {
                var path = guidCache.GetAssetFilePathsFromGuid(guid).FirstOrDefault();
                if (path == null)
                    return null;

                if (!module.TryGetFileByPath(path, out var sourceFile))
                    return null;

                return sourceFile;
            }
            else
            {
                return containingFile;
            }
        }
        
        public string GetScriptGuid(IPsiSourceFile file, string id)
        {
            return myScriptMapping[file].GetValuesSafe(id).FirstOrDefault();
        }
        
        public void Remove(IPsiSourceFile sourceFile, UnitySceneData sceneData)
        {
            myShellLocks.AssertMainThread();

            foreach (var (property, values) in sceneData.PropertiesData)
            {
                foreach (var value in values)
                {
                    myPropertyValueLocalCache.Remove(property, new MonoBehaviourPropertyValueWithLocation(sourceFile, value));
                }
            }
            
            foreach (var (id, element) in sceneData.SceneHierarchy.Elements)
            {
                mySceneElements.Remove(new SceneElementId(sourceFile, id), element);
            }
            
            foreach (var (name, ids) in sceneData.ShortNameToScriptFileId)
            {
                foreach (var id in ids)
                {
                    myShortNameToScriptTarget.Remove(name, (sourceFile, id));
                }
            }

            myScriptMapping.Remove(sourceFile);
        }
        
        public void ProcessSceneHierarchyFromComponentToRoot(IYamlDocument startComponent, IUnityCachedSceneProcessorConsumer consumer)
        {
            myShellLocks.AssertReadAccessAllowed();

            var sourceFile = startComponent.GetSourceFile();
            var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(startComponent.GetTextAsBuffer());
            if (sourceFile == null || anchor == null)
                return;
            
            ProcessSceneHierarchyFromComponentToRoot(sourceFile, new FileID(null, anchor),  consumer);
        }
        
        public void ProcessSceneHierarchyFromComponentToRoot(IPsiSourceFile sourceFile, string anchor, IUnityCachedSceneProcessorConsumer consumer)
        {
            myShellLocks.AssertReadAccessAllowed();
            ProcessSceneHierarchyFromComponentToRoot(sourceFile, new FileID(null, anchor),  consumer);
        }


        private void ProcessSceneHierarchyFromComponentToRoot(IPsiSourceFile sourceFile, FileID id, IUnityCachedSceneProcessorConsumer consumer)
        {
            var start = mySceneElements.GetValues(new SceneElementId(sourceFile, id)).FirstOrDefault();
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
                else if (start is GameObjectHierarchyElement gameObjectHierarchyElement)
                {
                    gameObjectId = gameObjectHierarchyElement.Id;
                } else
                {
                    Assertion.Fail("Unexpected hierarchy element");
                    return;
                }
                
                // Component must be attached to game object
                var gameObject = mySceneElements.GetValues(new SceneElementId(sourceFile, gameObjectId)).FirstOrDefault() as GameObjectHierarchyElement;
                if (gameObject == null)
                    return;
            
                // GameObject could be stripped, if another prefab's gameobject is modified via adding MonoBehaviour
                if (!gameObject.IsStripped)
                {
                    // Each GameObject must have Transform. We will use it to process scene hierarcy
                    var transform = mySceneElements.GetValues(new SceneElementId(sourceFile, gameObject.TransformId)).FirstOrDefault();
                    ProcessSceneHierarchyFromComponentToRootInner(sourceFile, transform, consumer, null);  
                } else 
                {
                    ProcessSceneHierarchyFromComponentToRootInner(sourceFile, gameObject, consumer, null);
                }
            }
            else
            {
                ProcessSceneHierarchyFromComponentToRootInner(sourceFile, start, consumer, null);
            }
        }
        
        
        // Invariant : startGameObject is Transform Component if it is not stripped 
        // This method traverse scene hierarchy via visiting transform components and push corresponding to transform GameObject into consumer
        private void ProcessSceneHierarchyFromComponentToRootInner(IPsiSourceFile currentFile, IUnityHierarchyElement startUnityObject,
            IUnityCachedSceneProcessorConsumer consumer, ModificationHierarchyElement modifications)
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
                    
                    
                    
                    if (correspondingId == null || prefabInstanceId == null)
                        return;

                    var prefabInstance = mySceneElements.GetValues(new SceneElementId(currentFile, prefabInstanceId)).FirstOrDefault() as ModificationHierarchyElement;
                    var prefabSourceFilePaths = myGuidCache.GetAssetFilePathsFromGuid(correspondingId.guid);
                    if (prefabSourceFilePaths.Count > 1 || prefabSourceFilePaths.Count == 0)
                        return;

                    var unityModule = currentFile.PsiModule as UnityExternalFilesPsiModule;
                    if (unityModule == null)
                        return;
                    
                    if (!unityModule.NotNull("externalFilesModuleFactory.PsiModule != null")
                        .TryGetFileByPath(prefabSourceFilePaths.First(), out var prefabSourceFile))
                        return;
                        

                    var prefabStartGameObject = mySceneElements.GetValues(new SceneElementId(prefabSourceFile, correspondingId.WithGuid(null))).FirstOrDefault();
                    
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
                                var sceneId = new SceneElementId(prefabSourceFile, componentHierarchyElement.GameObject);
                                attachedGameObject = mySceneElements.GetValues(sceneId).FirstOrDefault();
                            }

                            if (attachedGameObject is GameObjectHierarchyElement gameObjectHierarchyElement)
                            {
                                var sceneId = new SceneElementId(prefabSourceFile, gameObjectHierarchyElement.TransformId);
                                prefabStartGameObject = mySceneElements.GetValues(sceneId).FirstOrDefault();
                            }
                            else
                            {
                                prefabStartGameObject = null;
                            }
                        }
                    }
                    ProcessSceneHierarchyFromComponentToRootInner(prefabSourceFile, prefabStartGameObject, consumer, prefabInstance);
                    currentUnityObject = prefabInstance != null
                        ? mySceneElements.GetValues(new SceneElementId(currentFile, prefabInstance.TransformParentId)).FirstOrDefault()
                        : null;
                }
                else
                {
                    // assert that startGameObject is GameObject
                    var transformComponent = currentUnityObject as TransformHierarchyElement;
                    if (transformComponent == null)
                        return; // strange, log here
                    
                    Assertion.Assert(currentUnityObject is TransformHierarchyElement, "currentUnityObject is TransformHierarchyElement");



                    var fatherTransformId = new SceneElementId(currentFile, transformComponent.Father);
                    var father = mySceneElements.GetValues(fatherTransformId).FirstOrDefault();
                    
                    var goId = new SceneElementId(currentFile, transformComponent.GameObject);
                    
                    var gameObject = mySceneElements.GetValues(goId).FirstOrDefault() as GameObjectHierarchyElement;
                    if (gameObject == null)
                        return; // strange, log here
                    
                    consumer.ConsumeGameObject(gameObject, transformComponent, modifications);
                    currentUnityObject = father;
                }
            }
        }
        
        
        private struct SceneElementId
        {
            public readonly IPsiSourceFile SourceFile;
            public readonly FileID FileID;

            public SceneElementId(IPsiSourceFile sourceFile, FileID fileID)
            {
                SourceFile = sourceFile;
                FileID = fileID;
            }
            public bool Equals(SceneElementId other)
            {
                return SourceFile.Equals(other.SourceFile) && FileID.Equals(other.FileID);
            }

            public override bool Equals(object obj)
            {
                return obj is SceneElementId other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (SourceFile.GetHashCode() * 397) ^ FileID.GetHashCode();
                }
            }
        }
        
    }
}