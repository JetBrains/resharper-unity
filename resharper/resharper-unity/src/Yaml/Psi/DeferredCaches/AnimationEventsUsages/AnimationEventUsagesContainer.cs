using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages
{
    [SolutionComponent]
    public class AnimationEventUsagesContainer : IUnityAssetDataElementContainer
    {
        [NotNull] private readonly MetaFileGuidCache myMetaFileGuidCache;

        [NotNull] private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers =
            new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

        [NotNull] private readonly IShellLocks myShellLocks;

        [NotNull] private readonly CountingSet<Pair<string, Guid>> myUsagesCount =
            new CountingSet<Pair<string, Guid>>();

        [NotNull] private readonly OneToCompactCountingSet<Pair<string, Guid>, IPsiSourceFile> myUsageToSourceFiles =
            new OneToCompactCountingSet<Pair<string, Guid>, IPsiSourceFile>();

        public AnimationEventUsagesContainer([NotNull] IPersistentIndexManager manager,
                                             [NotNull] IShellLocks shellLocks,
                                             [NotNull] MetaFileGuidCache metaFileGuidCache)
        {
            myShellLocks = shellLocks;
            myMetaFileGuidCache = metaFileGuidCache;
        }

        public int Order => 0;

        public string Id => nameof(AnimationEventUsagesContainer);

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AnimationUsagesDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return currentAssetSourceFile.GetLocation().IsAnimFile();
        }

        public object Build(SeldomInterruptChecker checker,
                            IPsiSourceFile file,
                            AssetDocument assetDocument)
        {
            return new AnimationExtractor(file, assetDocument).TryExtractEventUsage();
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile,
                         AssetDocumentHierarchyElement assetDocumentHierarchyElement,
                         IUnityAssetDataElement element)
        {
            if (!(element is AnimationUsagesDataElement animationElement)) return;
            var usagesCount = myUsagesCount;
            foreach (var (functionNameAndGuid, events) in animationElement.FunctionNameAndGuidToEvents)
            {
                if (events is null) continue;
                var currentCount = usagesCount.GetCount(functionNameAndGuid);
                var eventsCount = events.Count;
                usagesCount.Add(functionNameAndGuid, eventsCount <= currentCount ? -eventsCount : -currentCount);
                myUsageToSourceFiles.Remove(functionNameAndGuid, currentAssetSourceFile);
            }

            myPointers.Remove(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile,
                          AssetDocumentHierarchyElement assetDocumentHierarchyElement,
                          IUnityAssetDataElementPointer unityAssetDataElementPointer,
                          IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;
            if (!(unityAssetDataElement is AnimationUsagesDataElement animationElement)) return;
            foreach (var (functionNameAndGuid, events) in animationElement.FunctionNameAndGuidToEvents)
            {
                if (events is null) continue;
                // ReSharper disable once AssignNullToNotNullAttribute
                myUsagesCount.Add(functionNameAndGuid, events.Count);
                myUsageToSourceFiles.Add(functionNameAndGuid, currentAssetSourceFile);
            }
        }

        public void Invalidate()
        {
            myUsageToSourceFiles.Clear();
            myUsagesCount.Clear();
            myPointers.Clear();
        }

        private void AssertShellLocks()
        {
            myShellLocks.AssertReadAccessAllowed();
        }

        [NotNull, ItemNotNull]
        public IEnumerable<AnimationUsage> GetEventUsagesFor([NotNull] IPsiSourceFile sourceFile,
                                                                  [NotNull] IMethod declaredElement)
        {
            AssertShellLocks();
            var boxedGuid = FindGuidOf(declaredElement);
            if (!boxedGuid.HasValue) return Enumerable.Empty<AnimationUsage>();
            var pointer = myPointers[sourceFile];
            if (pointer is null) return Enumerable.Empty<AnimationUsage>();
            var element = pointer.GetElement(sourceFile, Id);
            if (!(element is AnimationUsagesDataElement animatorElement))
                return Enumerable.Empty<AnimationUsage>();
            return GetEventUsagesFor(animatorElement, declaredElement.ShortName, boxedGuid.Value);
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<AnimationUsage> GetEventUsagesFor([NotNull] AnimationUsagesDataElement element,
                                                                          [NotNull] string functionName,
                                                                          Guid guid)
        {
            return element.FunctionNameAndGuidToEvents.GetValuesSafe(Pair.Of(functionName, guid));
        }

        [NotNull, ItemNotNull]
        public IEnumerable<IPsiSourceFile> GetPossibleFilesWithUsage([NotNull] IDeclaredElement element)
        {
            AssertShellLocks();
            if (!(element is IMethod method)) return EmptyList<IPsiSourceFile>.Enumerable;
            var guid = FindGuidOf(method);
            if (guid == null) return EmptyList<IPsiSourceFile>.Enumerable;
            return myUsageToSourceFiles.GetValues(Pair.Of(method.ShortName, guid.Value)) ?? 
                   EmptyList<IPsiSourceFile>.Enumerable;
        }

        
        public int GetEventUsagesCountFor([NotNull] IDeclaredElement element)
        {
            AssertShellLocks();
            if (!(element is IMethod method)) return 0;
            var guid = FindGuidOf(method);
            return guid != null ? myUsagesCount.GetCount(Pair.Of(method.ShortName, guid.Value)) : 0;
        }

        private Guid? FindGuidOf([NotNull] IClrDeclaredElement declaredElement)
        {
            return AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement.GetContainingType());
        }
    }
}