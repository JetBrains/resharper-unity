using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit
{
    [SolutionComponent]
    public class AnimImplicitUsagesContainer : IUnityAssetDataElementContainer
    {
         /*
         1. FindUsages on the methodname -> get script -> get gameobject -> get .controller-s attached to GO -> controller has cache of anim-s -> get anim-s -> anims should have cache of function-names.
         2. Suppress unused -> get all function names from all anims?
         3. Usages count?                    
         */
                
//        [NotNull] private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new();
        [NotNull] private readonly Dictionary<IPsiSourceFile, LocalList<AnimImplicitUsage>> myFileToEvents = new();
        [NotNull] private readonly CountingSet<string> myFunctionNames = new();

        [NotNull] private readonly IShellLocks myShellLocks;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly AnimatorScriptUsagesElementContainer myAnimatorScriptUsagesElementContainer;
        private readonly AnimatorGameObjectUsagesContainer myAnimatorGameObjectUsagesContainer;
        public int Order => 0;

        public string Id => nameof(AnimImplicitUsagesContainer);
        
        public AnimImplicitUsagesContainer(
            [NotNull] IShellLocks locks,
            MetaFileGuidCache metaFileGuidCache, 
            AnimatorScriptUsagesElementContainer animatorScriptUsagesElementContainer,
            AnimatorGameObjectUsagesContainer animatorGameObjectUsagesContainer)
        {
            myShellLocks = locks;
            myMetaFileGuidCache = metaFileGuidCache;
            myAnimatorScriptUsagesElementContainer = animatorScriptUsagesElementContainer;
            myAnimatorGameObjectUsagesContainer = animatorGameObjectUsagesContainer;
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
                var functionNameNode = functionRecord.GetMapEntry("functionName");
                var guid = AnimExtractor.ExtractEventFunctionGuidFrom(functionRecord);
                
                if (functionName != null && guid == null) // the case opposite to what does `JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages.AnimationExtractor.AddEvent`
                {
                    var nodeRange = functionNameNode!.GetTreeTextRange();
                    result.Add(new AnimImplicitUsage(new TextRange(assetDocument.StartOffset + nodeRange.StartOffset.Offset, assetDocument.StartOffset + nodeRange.EndOffset.Offset), file.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), functionName));
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
//            myPointers.Remove(currentAssetSourceFile);
            foreach (var usage in dataElement.Events)
            {
                myFunctionNames.Remove(usage.FunctionName);
            }
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile,
            AssetDocumentHierarchyElement assetDocumentHierarchyElement,
            IUnityAssetDataElementPointer unityAssetDataElementPointer,
            IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = (AnimImplicitUsagesDataElement)unityAssetDataElement;
//            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;

            if (!dataElement.Events.Any()) return;
            myFileToEvents[currentAssetSourceFile] = new LocalList<AnimImplicitUsage>(dataElement.Events); 
            foreach (var usage in dataElement.Events)
            {
                myFunctionNames.Add(usage.FunctionName);
            }
        }

        public void Invalidate()
        {
//            myPointers.Clear();
            myFileToEvents.Clear();
            myFunctionNames.Clear();
        }

        public bool LikelyUsed([NotNull] IMethod method)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (myFunctionNames.Contains(method.ShortName)) return true;
            return false;
        }

        public LocalList<AnimImplicitUsage> GetUsagesFor(IPsiSourceFile sourceFile, IDeclaredElement element)
        {
            myShellLocks.AssertReadAccessAllowed();
            
            var result = new LocalList<AnimImplicitUsage>();
            if (element is not IMethod method) return result;

            var shortName = method.ShortName;
            if (!myFunctionNames.Contains(shortName)) return result;
            
            var type = method.ContainingType;
            if (type is not IClass classType) return result;

            var solution = element.GetSolution();
            var container = solution.GetComponent<AssetScriptUsagesElementContainer>();
            var hierarchyElementContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();
            var scriptUsages = container.GetScriptUsagesFor(classType).ToArray(); // GO-s with script attached
            foreach (var scriptUsage in scriptUsages)
            {
                var he =  hierarchyElementContainer.GetHierarchyElement(scriptUsage.Location, true) as IComponentHierarchy; // GO

                // get Animators from that GameObject
                var controllerGuids =  myAnimatorGameObjectUsagesContainer.GetAnimatorsFromGameObject(he.OwningGameObject);
                var controllers = controllerGuids.SelectMany(a => myMetaFileGuidCache.GetAssetFilePathsFromGuid(a)).ToArray();

                foreach (var file in controllers)
                {
                    var anims = myAnimatorScriptUsagesElementContainer.GetAnimReferences(file).SelectMany(a=>myMetaFileGuidCache.GetAssetFilePathsFromGuid(a)).ToArray();

                    foreach (var fileToEvent in myFileToEvents)
                    {
                        if (anims.Contains(fileToEvent.Key.GetLocation()))
                        {
                            result.AddRange(fileToEvent.Value);
                        }
                    }
                }
            }

            return result;
        }

        public int GetEventUsagesCountFor(IDeclaredElement element, out bool expected)
        {
            myShellLocks.AssertReadAccessAllowed();
            expected = false;
            if (element is not IMethod method) return 0;
            if (myFunctionNames.Contains(method.ShortName)) 
                expected = true;
            return 0; // todo: need a fast way to get real count
        }
        
        [NotNull]
        [ItemNotNull]
        public IEnumerable<IPsiSourceFile> GetPossibleFilesWithUsage([NotNull] IDeclaredElement element)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (element is not IClrDeclaredElement clrDeclaredElement) return EmptyList<IPsiSourceFile>.Enumerable;
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, clrDeclaredElement.GetContainingType());
            if (!guid.HasValue) return EmptyList<IPsiSourceFile>.Enumerable;
            
            var name = element.ShortName;

            var files = new List<IPsiSourceFile>();
            foreach (var fileToEvent in myFileToEvents)
            {
                if (fileToEvent.Value.ToArray().Select(a=>a.FunctionName).Contains(name))
                    files.Add(fileToEvent.Key);
            }

            return files;
        }
    }
}