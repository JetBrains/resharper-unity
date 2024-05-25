using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit
{
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class AnimImplicitUsagesContainer : IUnityAssetDataElementContainer
    {
        [NotNull] private readonly Dictionary<IPsiSourceFile, LocalList<AnimImplicitUsage>> myFileToEvents = new();
        [NotNull] private readonly CompactOneToSetMap<string, IPsiSourceFile> myEventNameToFiles = new();

        [NotNull] private readonly IShellLocks myShellLocks;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly AssetScriptUsagesElementContainer myAssetScriptUsagesElementContainer;
        private readonly AnimatorScriptUsagesElementContainer myAnimatorScriptUsagesElementContainer;
        private readonly AnimatorGameObjectUsagesContainer myAnimatorGameObjectUsagesContainer;
        private readonly AssetDocumentHierarchyElementContainer myAssetDocumentHierarchyElementContainer;
        public int Order => 0;

        public string Id => nameof(AnimImplicitUsagesContainer);

        public AnimImplicitUsagesContainer(
            [NotNull] IShellLocks locks,
            MetaFileGuidCache metaFileGuidCache,
            AssetScriptUsagesElementContainer assetScriptUsagesElementContainer,
            AnimatorScriptUsagesElementContainer animatorScriptUsagesElementContainer,
            AnimatorGameObjectUsagesContainer animatorGameObjectUsagesContainer,
            AssetDocumentHierarchyElementContainer assetDocumentHierarchyElementContainer)
        {
            myShellLocks = locks;
            myMetaFileGuidCache = metaFileGuidCache;
            myAssetScriptUsagesElementContainer = assetScriptUsagesElementContainer;
            myAnimatorScriptUsagesElementContainer = animatorScriptUsagesElementContainer;
            myAnimatorGameObjectUsagesContainer = animatorGameObjectUsagesContainer;
            myAssetDocumentHierarchyElementContainer = assetDocumentHierarchyElementContainer;
        }

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AnimImplicitUsagesDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return currentAssetSourceFile.IsAnim();
        }

        public object Build(IPsiSourceFile file, AssetDocument assetDocument)
        {
            var result = new LocalList<AnimImplicitUsage>();

            assetDocument = AnimExtractor.GetAnimationEventsDocument(assetDocument);
            if (assetDocument == null)
                return null;

            var events = AnimExtractor.GetAnimationEventsNode(assetDocument);
            if (events == null) return null;

            foreach (var @event in events!.Entries)
            {
                if (@event?.Value is not IBlockMappingNode functionRecord) continue;
                var functionName = AnimExtractor.ExtractEventFunctionNameFrom(functionRecord);
                var guid = AnimExtractor.ExtractEventFunctionGuidFrom(functionRecord);
                
                // the case opposite to what does `JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages.AnimationExtractor.AddEvent`
                if (functionName != null && guid == null) 
                {
                    var functionNameNode = functionRecord.GetMapEntry("functionName");
                    var contentOffset = functionNameNode!.Content.Value.GetTreeTextRange().StartOffset.Offset;
                    // we don't actually use LocalReference
                    result.Add(new AnimImplicitUsage(LocalReference.Null, new TextRange(assetDocument.StartOffset + contentOffset, assetDocument.StartOffset + contentOffset), file.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), functionName));
                }
            }

            return result;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile,
            AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimImplicitUsagesDataElement)unityAssetDataElement;
            myFileToEvents.Remove(currentAssetSourceFile);
            foreach (var usage in dataElement.Events)
            {
                myEventNameToFiles.RemoveKey(usage.FunctionName);
            }
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile,
            AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElementPointer unityAssetDataElementPointer,
            IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimImplicitUsagesDataElement)unityAssetDataElement;
            myFileToEvents[currentAssetSourceFile] = new LocalList<AnimImplicitUsage>(dataElement.Events);
            foreach (var usage in dataElement.Events)
            {
                myEventNameToFiles.Add(usage.FunctionName, currentAssetSourceFile);
            }
        }

        public void Invalidate()
        {
            myFileToEvents.Clear();
            myEventNameToFiles.Clear();
        }

        public bool LikelyUsed([NotNull] IMethod method)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (myEventNameToFiles[method.ShortName].Any()) return true;
            return false;
        }

        public HashSet<AnimImplicitUsage> GetUsagesFor(IPsiSourceFile sourceFile, IDeclaredElement element)
        {
            // we are using both strait and reversed to support more prefab modification cases
            var animImplicitUsages = GetUsagesForStraight(sourceFile, element);
            var reversedUsages = GetUsagesForReversed(sourceFile, element);
            return animImplicitUsages.Union(reversedUsages).ToHashSet();
        }

        // sourceFile (one of the anims workIndex) -> guid -> controller -> gameobject -> script -> method by name
        private HashSet<AnimImplicitUsage> GetUsagesForReversed(IPsiSourceFile sourceFile, IDeclaredElement element)
        {
            var result = new HashSet<AnimImplicitUsage>();
            if (!IsApplicable(sourceFile)) return result;

            if (element is not IMethod method) return result;
            var shortName = method.ShortName;
            var type = method.ContainingType;
            if (type is not IClass classType) return result;
            if (!classType.DerivesFromMonoBehaviour()) return result;
            if (!myFileToEvents[sourceFile].Any(a => a.FunctionName == shortName)) return result;

            var animGuid = myMetaFileGuidCache.GetAssetGuid(sourceFile);
            if (!animGuid.HasValue) return result;
            var controllerFiles = myAnimatorScriptUsagesElementContainer.GetControllerFileByAnimGuid(animGuid.Value);
            var controllerGuids = controllerFiles
                .Select(controllerFile => myMetaFileGuidCache.GetAssetGuid(controllerFile)).ToArray();
            var references = controllerGuids.SelectMany(controllerGuid =>
                myAnimatorGameObjectUsagesContainer.GetGameObjectReferencesByControllerGuid(controllerGuid)).ToArray();
            Interruption.Current.CheckAndThrow();
            var gameObjects = references.SelectMany(reference => 
                AssetHierarchyUtil.GetSelfAndOriginalGameObjects(reference, myAssetDocumentHierarchyElementContainer)).ToArray();

            Interruption.Current.CheckAndThrow();
            var scriptUsages = myAssetScriptUsagesElementContainer.GetScriptUsagesFor(classType).ToArray();
            var gameObjectsFromScript = scriptUsages.SelectMany(scriptUsage =>
                AssetHierarchyUtil.GetSelfAndOriginalGameObjects(scriptUsage.Location, myAssetDocumentHierarchyElementContainer));

            if (gameObjects.Any(go => gameObjectsFromScript.Contains(go)))
            {
                var animImplicitUsages = myFileToEvents[sourceFile].ToArray().Where(a => a.FunctionName == shortName);
                result.AddRange(animImplicitUsages);
            }

            return result;
        }

        // methodname -> get script -> get gameobject -> get .controller-s attached to GO -> controller has cache of anim-s -> get anim-s -> anims should have cache of function-names.
        private HashSet<AnimImplicitUsage> GetUsagesForStraight(IPsiSourceFile sourceFile, IDeclaredElement element)
        {
            myShellLocks.AssertReadAccessAllowed();
            var result = new HashSet<AnimImplicitUsage>();
            if (!IsApplicable(sourceFile)) return result;

            if (element is not IMethod method) return result;
            var shortName = method.ShortName;
            var type = method.ContainingType;
            if (type is not IClass classType) return result;
            if (!classType.DerivesFromMonoBehaviour()) return result;
            if (!myFileToEvents[sourceFile].Any(a => a.FunctionName == shortName)) return result;

            var scriptUsages = myAssetScriptUsagesElementContainer.GetScriptUsagesFor(classType).ToArray(); // GO-s with script attached
            foreach (var scriptUsage in scriptUsages)
            {
                Interruption.Current.CheckAndThrow();
                var gameObjects = AssetHierarchyUtil.GetSelfAndOriginalGameObjects(scriptUsage.Location, myAssetDocumentHierarchyElementContainer);

                var controllerGuids = myAnimatorGameObjectUsagesContainer.GetAnimatorsByGameObjectReference(gameObjects).Distinct();
                var controllers = controllerGuids.SelectMany(a => myMetaFileGuidCache.GetAssetFilePathsFromGuid(a)).Distinct().ToArray();
                if (controllers.Any(controller =>
                    {
                        return myAnimatorScriptUsagesElementContainer.GetAnimReferences(controller)
                            .SelectMany(a => myMetaFileGuidCache.GetAssetFilePathsFromGuid(a))
                            .Any(b => b.Equals(sourceFile.GetLocation()));
                    }))
                {
                    var animImplicitUsages = myFileToEvents[sourceFile].ToArray().Where(a => a.FunctionName == shortName);
                    result.AddRange(animImplicitUsages);
                }
            }

            return result;
        }

        public int GetEventUsagesCountFor(IDeclaredElement element, out bool expected)
        {
            myShellLocks.AssertReadAccessAllowed();
            expected = false;
            if (element is not IMethod method) return 0;
            var type = method.ContainingType;
            if (type is not IClass classType) return 0;
            if (!classType.DerivesFromMonoBehaviour()) return 0;
            if (myEventNameToFiles[method.ShortName].Any())
                expected = true;
            return 0; // todo: need a fast way to get real count
        }

        [NotNull]
        [ItemNotNull]
        public IEnumerable<IPsiSourceFile> GetPossibleFilesWithUsage([NotNull] IDeclaredElement element)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (element is not IMethod method) return Enumerable.Empty<IPsiSourceFile>();
            var type = method.ContainingType;
            if (type is not IClass classType) return Enumerable.Empty<IPsiSourceFile>();
            if (!classType.DerivesFromMonoBehaviour()) return Enumerable.Empty<IPsiSourceFile>();

            return myEventNameToFiles[method.ShortName];

        }
    }
}