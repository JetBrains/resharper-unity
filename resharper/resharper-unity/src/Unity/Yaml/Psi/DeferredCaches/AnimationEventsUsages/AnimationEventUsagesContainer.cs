using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
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

        [NotNull] private readonly ISolution mySolution;

        [NotNull] private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers =
            new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

        [NotNull] private readonly IShellLocks myShellLocks;

        [NotNull] private readonly OneToCompactCountingSet<string, Guid> myNameToGuids =
            new OneToCompactCountingSet<string, Guid>();

        [NotNull] private readonly OneToCompactCountingSet<Pair<string, Guid>, IPsiSourceFile> myUsageToSourceFiles =
            new OneToCompactCountingSet<Pair<string, Guid>, IPsiSourceFile>();

        public AnimationEventUsagesContainer([NotNull] IPersistentIndexManager manager,
                                             [NotNull] IShellLocks shellLocks,
                                             [NotNull] MetaFileGuidCache metaFileGuidCache,
                                             [NotNull] ISolution solution)
        {
            myShellLocks = shellLocks;
            myMetaFileGuidCache = metaFileGuidCache;
            mySolution = solution;
        }

        public int Order => 0;

        public string Id => nameof(AnimationEventUsagesContainer);

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AnimationUsagesDataElement(mySolution, myMetaFileGuidCache);
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return currentAssetSourceFile.IsAnim();
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
            foreach (var @event in animationElement.Events)
            {
                var functionName = @event.FunctionName;
                var guid = @event.Guid;
                var currentCount = myNameToGuids.GetCount(functionName, guid);
                if (currentCount != 0) myNameToGuids.Remove(functionName, guid);
                myUsageToSourceFiles.Remove(Pair.Of(functionName, guid), currentAssetSourceFile);
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
            foreach (var @event in animationElement.Events)
            {
                myNameToGuids.Add(@event.FunctionName, @event.Guid);
                myUsageToSourceFiles.Add(Pair.Of(@event.FunctionName, @event.Guid), currentAssetSourceFile);
            }
        }

        public void Invalidate()
        {
            myUsageToSourceFiles.Clear();
            myNameToGuids.Clear();
            myPointers.Clear();
        }

        private void AssertShellLocks()
        {
            myShellLocks.AssertReadAccessAllowed();
        }

        [NotNull]
        [ItemNotNull]
        public IEnumerable<AnimationUsage> GetEventUsagesFor([NotNull] IPsiSourceFile sourceFile,
                                                             [NotNull] IDeclaredElement declaredElement)
        {
            AssertShellLocks();
            if (!(declaredElement is IClrDeclaredElement clrDeclaredElement))
                return EmptyList<AnimationUsage>.Enumerable;
            var boxedGuid = FindGuidOf(clrDeclaredElement);
            if (!boxedGuid.HasValue) return Enumerable.Empty<AnimationUsage>();
            var pointer = myPointers[sourceFile];
            if (pointer is null) return Enumerable.Empty<AnimationUsage>();
            var element = pointer.GetElement(sourceFile, Id);
            if (!(element is AnimationUsagesDataElement animatorElement)) return Enumerable.Empty<AnimationUsage>();
            var name = declaredElement.ShortName;
            var containingType = clrDeclaredElement.GetContainingType();
            var solution = mySolution;
            return animatorElement.Events
                .Where(usage => name.Equals(usage.FunctionName))
                .Select(usage => new
                {
                    usage, typeElement = AssetUtils.GetTypeElementFromScriptAssetGuid(solution, usage.Guid)
                })
                .Where(t => t.typeElement != null && t.typeElement.IsDescendantOf(containingType))
                .Select(t => t.usage);
        }

        [NotNull]
        [ItemNotNull]
        public IEnumerable<IPsiSourceFile> GetPossibleFilesWithUsage([NotNull] IDeclaredElement element)
        {
            AssertShellLocks();
            if (!(element is IClrDeclaredElement clrDeclaredElement)) return EmptyList<IPsiSourceFile>.Enumerable;
            var elementGuid = FindGuidOf(clrDeclaredElement);
            if (!elementGuid.HasValue) return EmptyList<IPsiSourceFile>.Enumerable;
            // TODO: Fix. Search for subtypes
            var name = element.ShortName;
            var found = myNameToGuids.TryGetValue(name, out var guidsAndCounts);
            if (!found) return EmptyList<IPsiSourceFile>.Enumerable;
            var elementType = clrDeclaredElement.GetContainingType();
            var descendentTypesGuids = guidsAndCounts
                .Select(guidAndCount => guidAndCount.Key)
                .Where(g => AssetUtils.GetTypeElementFromScriptAssetGuid(mySolution, g)?.IsDescendantOf(elementType) ??
                            false);
            var files = new List<IPsiSourceFile>();
            files.AddRange(myUsageToSourceFiles.GetValues(Pair.Of(name, elementGuid.Value)));
            foreach (var descendentTypesGuid in descendentTypesGuids)
            {
                files.AddRange(myUsageToSourceFiles.GetValues(Pair.Of(name, descendentTypesGuid)));
            }

            return files;
        }

        public int GetEventUsagesCountFor([NotNull] IDeclaredElement element, out bool estimated)
        {
            AssertShellLocks();
            estimated = false;
            if (!(element is IClrDeclaredElement clrDeclaredElement)) return 0;

            var type = clrDeclaredElement.GetContainingType();
            switch (element)
            {
                case IMethod method:
                    return GetEventUsagesCountFor(method, type, ref estimated);
                case IProperty property:
                    return GetEventPropertyUsagesCountFor(property, type, ref estimated);
            }

            return 0;
        }

        private int GetEventPropertyUsagesCountFor([NotNull] IProperty property,
                                                   [CanBeNull] ITypeElement containingType,
                                                   ref bool estimated)
        {
            var count = 0;
            var getter = property.Getter;
            if (getter != null) count += GetEventUsagesCountFor(getter, containingType, ref estimated);
            var setter = property.Setter;
            if (setter != null) count += GetEventUsagesCountFor(setter, containingType, ref estimated);
            return count;
        }

        private int GetEventUsagesCountFor([NotNull] IDeclaredElement element,
                                           [CanBeNull] ITypeElement containingType, ref bool estimated)
        {
            var found = myNameToGuids.TryGetValue(element.ShortName, out var guids);
            if (!found) return 0;
            var count = 0;
            const int maxProcessCount = 5;
            foreach (var (guid, guidCount) in guids.Take(maxProcessCount))
            {
                var typeElement = AssetUtils.GetTypeElementFromScriptAssetGuid(mySolution, guid);
                if (typeElement == null || !typeElement.IsDescendantOf(containingType)) continue;
                count += guidCount;
            }

            if (guids.Count > maxProcessCount) estimated = true;
            return count;
        }

        private Guid? FindGuidOf([NotNull] IClrDeclaredElement declaredElement)
        {
            return AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement.GetContainingType());
        }
    }
}