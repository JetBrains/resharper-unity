using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [SolutionComponent]
    public class UnitySceneProcessor
    {
        private readonly UnityVersion myVersion;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly UnityExternalFilesModuleFactory myFactory;

        public UnitySceneProcessor(UnityVersion version, MetaFileGuidCache metaFileGuidCache, UnityExternalFilesModuleFactory factory)
        {
            myVersion = version;
            myMetaFileGuidCache = metaFileGuidCache;
            myFactory = factory;
        }

        private static bool IsStripped(IYamlDocument element)
        {
            return ((element.Body.BlockNode as IChameleonBlockMappingNode)?.Properties?.LastChild as YamlTokenType.GenericTokenElement)?
                   .GetText().Equals("stripped") == true;
        }
        
        public void ProcessSceneHierarchyFromComponentToRoot(IYamlDocument startComponent, IUnitySceneProcessorConsumer consumer)
        {
            if (startComponent == null)
                return;

            var start = startComponent;
            if (!IsStripped(startComponent)) // start component can be stripped, e.g : prefab's MonoBehavior's function is passed to button event handler
            {
                 // Component must be attached to game object
                start = start.GetUnityObjectDocumentFromFileIDProperty(UnityYamlConstants.GameObjectProperty);
                
                // GameObject could be stripped, if another prefab's gameobject is modified via adding MonoBehaviour
                if (!IsStripped(start))
                {
                    // Each GameObject must have Transform. We will use it to process scene hierarcy
                    start = UnityObjectPsiUtil.FindTransformComponentForGameObject(start);
                }
            }
            ProcessSceneHierarchyFromComponentToRootInner(start, consumer, null);   
        }

        
        // Invariant : startGameObject is Transform Component if it is not stripped 
        // This method traverse scene hierarchy via visiting transform components and push corresponding to transform GameObject into consumer
        private void ProcessSceneHierarchyFromComponentToRootInner(IYamlDocument startUnityObject, IUnitySceneProcessorConsumer consumer, IBlockMappingNode modifications)
        {
            var currentUnityObject = startUnityObject;
            while (currentUnityObject != null)
            {
                // Unity object could be stripped, it means, that corresponding real object belongs to another yaml file
                // Also, it has reference to prefab instance in current file, which stores all prefab modification
                if (IsStripped(currentUnityObject))
                {
                    var file = (IYamlFile) currentUnityObject.GetContainingFile();
                    var correspondingId = GetCorrespondingSourceObjectFileId(currentUnityObject);
                    var prefabInstanceId = GetPrefabInstanceFileId(currentUnityObject);
                    
                    // assert not null
                    if (correspondingId == null || prefabInstanceId == null)
                        return;
                    
                    var prefabInstance = file.FindDocumentByAnchor(prefabInstanceId.fileID);
                    var prefabSourceFile = myMetaFileGuidCache.GetAssetFilePathsFromGuid(correspondingId.guid);
                    if (prefabSourceFile.Count > 1 || prefabSourceFile.Count == 0)
                        return;

                    myFactory.PsiModule.NotNull("externalFilesModuleFactory.PsiModule != null")
                        .TryGetFileByPath(prefabSourceFile.First(), out var sourceFile);

                    if (sourceFile == null)
                        return;
                    
                    // [TODO] Is prefab file committed???
                    var prefabFile = (IYamlFile)sourceFile.GetDominantPsiFile<YamlLanguage>();

                    var prefabStartGameObject = prefabFile.FindDocumentByAnchor(correspondingId.fileID);
                    
                    if (prefabStartGameObject == null)
                        return; // TODO [vkrasnotsvetov] 19.1 Handle case, when prefab contains prefab which contains prefab
                    
                    if (!IsStripped(prefabStartGameObject))
                    {
                        // !u!4 is transform. If tag is different, let's extract transform, there are two cases:
                        // 1) prefabStartGameObject is GameObject(!u!1), take its transform
                        // 2) prefabStartGameObject is Component, so get attached gameobject and from this gameobject take transform component
                        if (!GetUnityObjectTag(prefabStartGameObject).Equals("!u!4"))
                        {
                            var attachedGameObject= prefabStartGameObject;
                            if (!GetUnityObjectTag(prefabStartGameObject).Equals("!u!1"))
                            {
                                attachedGameObject =
                                    attachedGameObject.GetUnityObjectDocumentFromFileIDProperty(UnityYamlConstants
                                        .GameObjectProperty);
                            }
                            prefabStartGameObject = UnityObjectPsiUtil.FindTransformComponentForGameObject(attachedGameObject);
                        }
                    }
                    var localModifications = UnityObjectPsiUtil.GetPrefabModification(prefabInstance);
                    ProcessSceneHierarchyFromComponentToRootInner(prefabStartGameObject, consumer, localModifications);
                    currentUnityObject = UnityObjectPsiUtil.GetTransformFromPrefabInstance(prefabInstance);
                }
                else
                {
                    // assert that startGameObject is GameObject
                    var father = currentUnityObject.GetUnityObjectDocumentFromFileIDProperty(UnityYamlConstants.FatherProperty);
                    var gameObject = currentUnityObject.GetUnityObjectDocumentFromFileIDProperty(UnityYamlConstants.GameObjectProperty);
                    consumer.ConsumeGameObject(gameObject, modifications);
                    currentUnityObject = father;
                }
            }
        }

        public static string GetUnityObjectTag(IYamlDocument document)
        {
            var tag = (document.Body.BlockNode as IChameleonBlockMappingNode)?.Properties.TagProperty.GetText();
            return tag;
        }
        
        private FileID GetCorrespondingSourceObjectFileId(IYamlDocument document)
        {
            if (myVersion.GetActualVersionForSolution().Major == 2017)
                return document.GetUnityObjectPropertyValue(UnityYamlConstants.CorrespondingSourceObjectProperty2017)?.AsFileID();
            
            return document.GetUnityObjectPropertyValue(UnityYamlConstants.CorrespondingSourceObjectProperty)?.AsFileID() ??
                   document.GetUnityObjectPropertyValue(UnityYamlConstants.CorrespondingSourceObjectProperty2017)?.AsFileID();
    }
        
        private FileID GetPrefabInstanceFileId(IYamlDocument document)
        {
            if (myVersion.GetActualVersionForSolution().Major == 2017)
                return document.GetUnityObjectPropertyValue(UnityYamlConstants.PrefabInstanceProperty2017)?.AsFileID();
            
            return document.GetUnityObjectPropertyValue(UnityYamlConstants.PrefabInstanceProperty)?.AsFileID() ??
                   document.GetUnityObjectPropertyValue(UnityYamlConstants.PrefabInstanceProperty2017)?.AsFileID();
        }
    }

    public interface IUnitySceneProcessorConsumer
    {
        void ConsumeGameObject(IYamlDocument gameObject, IBlockMappingNode modifications);
    }
}