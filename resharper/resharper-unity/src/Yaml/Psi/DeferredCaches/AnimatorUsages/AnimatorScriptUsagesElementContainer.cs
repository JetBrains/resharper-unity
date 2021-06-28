using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    [SolutionComponent]
    public class AnimatorScriptUsagesElementContainer : IScriptUsagesElementContainer
    {
        [NotNull] private readonly IPersistentIndexManager myManager;
        [NotNull] private readonly MetaFileGuidCache myMetaFileGuidCache;

        [NotNull] private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers =
            new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

        [NotNull] private readonly IShellLocks myShellLocks;
        [NotNull] private readonly CountingSet<string> myStateNamesCount = new CountingSet<string>();

        [NotNull] private readonly CountingSet<Guid> myUsagesCount = new CountingSet<Guid>();

        [NotNull] private readonly OneToCompactCountingSet<Guid, IPsiSourceFile> myUsageToSourceFiles =
            new OneToCompactCountingSet<Guid, IPsiSourceFile>();

        public AnimatorScriptUsagesElementContainer([NotNull] IPersistentIndexManager manager,
                                                    [NotNull] IShellLocks shellLocks,
                                                    [NotNull] MetaFileGuidCache metaFileGuidCache)
        {
            myManager = manager;
            myShellLocks = shellLocks;
            myMetaFileGuidCache = metaFileGuidCache;
        }

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AnimatorUsagesDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return currentAssetSourceFile.GetLocation().IsControllerFile();
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile file, AssetDocument document)
        {
            if (AssetUtils.IsMonoBehaviourDocument(document.Buffer))
                return ExtractStateMachineBehaviour(document, file);
            var animatorExtractor = new AnimatorExtractor(file, document);
            if (AssetUtils.IsAnimatorStateMachine(document.Buffer)) return animatorExtractor.TryExtractStateMachine();
            return AssetUtils.IsAnimatorState(document.Buffer) ? animatorExtractor.TryExtractUsage() : null;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile,
                         AssetDocumentHierarchyElement assetDocumentHierarchyElement,
                         IUnityAssetDataElement element)
        {
            if (!(element is AnimatorUsagesDataElement animatorElement)) return;
            var usagesCount = myUsagesCount;
            var usageToSourceFiles = myUsageToSourceFiles;
            foreach (var (guid, anchors) in animatorElement.GuidToAnchors)
            {
                if (anchors is null) continue;
                var currentCount = usagesCount.GetCount(guid);
                var anchorsCount = anchors.Count;
                usagesCount.Add(guid, anchorsCount <= currentCount ? -anchorsCount : -currentCount);
                usageToSourceFiles.Remove(guid, currentAssetSourceFile);
            }

            myPointers.Remove(currentAssetSourceFile);
            foreach (var stateName in animatorElement.StateNames) myStateNamesCount.Remove(stateName);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile,
                          AssetDocumentHierarchyElement assetDocumentHierarchyElement,
                          IUnityAssetDataElementPointer unityAssetDataElementPointer,
                          IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;
            if (!(unityAssetDataElement is AnimatorUsagesDataElement animatorElement)) return;
            foreach (var (guid, anchors) in animatorElement.GuidToAnchors)
            {
                if (anchors is null) continue;
                // ReSharper disable once AssignNullToNotNullAttribute
                myUsagesCount.Add(guid, anchors.Count);
                myUsageToSourceFiles.Add(guid, currentAssetSourceFile);
            }

            var stateNames = animatorElement.StateNames;
            if (stateNames.Count == 0) return;
            foreach (var stateName in stateNames) myStateNamesCount.Add(stateName);
        }

        public string Id => nameof(AnimatorScriptUsagesElementContainer);

        public int Order => 0;

        public void Invalidate()
        {
            myUsageToSourceFiles.Clear();
            myUsagesCount.Clear();
            myPointers.Clear();
            myStateNamesCount.Clear();
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithScriptUsages(IClass scriptClass)
        {
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, scriptClass);
            return guid != null
                ? GetPossibleFilesWithScriptUsages(myUsageToSourceFiles.GetValues(guid.Value))
                : new LocalList<IPsiSourceFile>();
        }

        public int GetScriptUsagesCount(IClassLikeDeclaration classLikeDeclaration, out bool estimatedResult)
        {
            AssertShellLocks();
            estimatedResult = false;
            var sourceFile = classLikeDeclaration.GetSourceFile();
            if (sourceFile == null) return 0;
            var declaredElement = classLikeDeclaration.DeclaredElement;
            if (declaredElement == null) return 0;
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            return guid != null ? myUsagesCount.GetCount(guid.Value) : 0;
        }

        public IEnumerable<IScriptUsage> GetScriptUsagesFor(IPsiSourceFile sourceFile,
                                                            ITypeElement declaredElement)
        {
            AssertShellLocks();
            var boxedGuid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            if (!boxedGuid.HasValue) return Enumerable.Empty<IScriptUsage>();
            var unityAssetDataElementPointer = myPointers[sourceFile];
            if (unityAssetDataElementPointer is null) return Enumerable.Empty<IScriptUsage>();
            var element = unityAssetDataElementPointer.GetElement(sourceFile, Id);
            if (!(element is AnimatorUsagesDataElement animatorElement)) return Enumerable.Empty<IScriptUsage>();
            return GetScriptUsagesFor(animatorElement, boxedGuid.Value);
        }

        [NotNull]
        [ItemNotNull]
        public IEnumerable<string> GetStateNames()
        {
            AssertShellLocks();
            return myStateNamesCount.GetItems();
        }

        public bool ContainsStateName([NotNull] string stateName)
        {
            AssertShellLocks();
            return myStateNamesCount.Contains(stateName);
        }

        private static LocalList<IPsiSourceFile> GetPossibleFilesWithScriptUsages(IEnumerable<IPsiSourceFile> files)
        {
            return files?.Aggregate(new LocalList<IPsiSourceFile>(), AddFile) ?? new LocalList<IPsiSourceFile>();
        }

        private static LocalList<IPsiSourceFile> AddFile(LocalList<IPsiSourceFile> files, [NotNull] IPsiSourceFile file)
        {
            files.Add(file);
            return files;
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<IScriptUsage> GetScriptUsagesFor([NotNull] AnimatorUsagesDataElement element,
                                                                    Guid guid)
        {
            var stateUsages = GetUsages(element, guid, element.ScriptAnchorToStateUsages);
            var stateMachineUsages = GetUsages(element, guid, element.ScriptAnchorToStateMachineUsages);
            return ConcatUsages(stateUsages, stateMachineUsages);
        }

        [CanBeNull]
        public string GetStateMachinePathFor(LocalReference location)
        {
            AssertShellLocks();
            var element = GetDataElement(location);
            if (element is null) return null;
            var namesConsumer = new PathConsumer<string>(usage => usage.Name);
            ProcessPath(element, namesConsumer, location.LocalDocumentAnchor);
            var names = namesConsumer.Elements;
            names.Reverse();
            return names.Join("/");
        }

        public bool GetElementsNames(LocalReference location,
                                     [NotNull] IDeclaredElement declaredElement,
                                     [CanBeNull] out string[] names,
                                     out bool isStateMachine)
        {
            AssertShellLocks();
            var element = GetDataElement(location);
            if (element is null)
            {
                names = null;
                isStateMachine = false;
                return false;
            }

            var namesConsumer = new PathConsumer<string>(usage => usage.Name);
            var anchor = location.LocalDocumentAnchor;
            var namesAndAnchors = namesConsumer.Elements;
            AddBottomElement(element, namesConsumer, anchor, declaredElement, out isStateMachine);
            ProcessPath(element, namesConsumer, anchor);
            namesAndAnchors.Reverse();
            names = namesAndAnchors.ToArray();
            return true;
        }

        private void AddBottomElement([NotNull] AnimatorUsagesDataElement element,
                                      [NotNull] PathConsumer<string> namesConsumer,
                                      long anchor,
                                      [NotNull] IDeclaredElement declaredElement,
                                      out bool isStateMachine)
        {
            if (TryAddBottomStateMachine(element, namesConsumer, anchor))
            {
                isStateMachine = true;
                return;
            }

            isStateMachine = false;
            AddBottomStateElement(element, namesConsumer, anchor, declaredElement);
        }

        private static bool TryAddBottomStateMachine([NotNull] AnimatorUsagesDataElement element,
                                                     [NotNull] PathConsumer<string> namesConsumer,
                                                     long anchor)
        {
            if (!element.StateMachineAnchorToUsage.TryGetValue(anchor, out var bottomElement) ||
                bottomElement is null) return false;
            namesConsumer.Elements.Add(bottomElement.Name);
            return true;
        }

        private void AddBottomStateElement([NotNull] AnimatorUsagesDataElement element,
                                           [NotNull] PathConsumer<string> namesConsumer,
                                           long anchor,
                                           [NotNull] IDeclaredElement declaredElement)
        {
            if (!(declaredElement is ITypeElement typeElement)) return;
            var boxedGuid = AssetUtils.GetGuidFor(myMetaFileGuidCache, typeElement);

            if (!boxedGuid.HasValue) return;
            var name = GetUsages(element, boxedGuid.Value, element.ScriptAnchorToStateUsages)
                .Where(usage => usage.Location.LocalDocumentAnchor == anchor)
                .Select(usage => usage.Name)
                .FirstOrDefault();
            namesConsumer.Elements.Add(name);
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<T> GetUsages<T>(
            [NotNull] AnimatorUsagesDataElement element,
            Guid boxedGuid,
            [NotNull] OneToListMap<long, T> d) where T : IScriptUsage
        {
            var stateUsages = new List<T>();
            foreach (var scriptAnchor in element.GuidToAnchors.GetValuesSafe(boxedGuid))
                stateUsages.AddRange(d.GetValuesSafe(scriptAnchor));
            return stateUsages;
        }

        [CanBeNull]
        private AnimatorUsagesDataElement GetDataElement(LocalReference location)
        {
            var sourceFile = myManager[location.OwningPsiPersistentIndex];
            if (sourceFile == null) return null;
            return myPointers[sourceFile]?.GetElement(sourceFile, Id) as AnimatorUsagesDataElement;
        }

        private static void ProcessPath<T>([NotNull] AnimatorUsagesDataElement element,
                                           [NotNull] PathConsumer<T> consumer,
                                           long currentAnchor)
        {
            var childToParent = element.ChildToParent;
            var anchorToStateMachineUsage = element.StateMachineAnchorToUsage;
            while (childToParent.TryGetValue(currentAnchor, out var parent) &&
                   anchorToStateMachineUsage.TryGetValue(parent, out var usage) &&
                   usage != null)
            {
                consumer.Consume(usage);
                currentAnchor = parent;
            }
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<IScriptUsage> ConcatUsages(
            [NotNull] IEnumerable<AnimatorStateScriptUsage> stateUsages,
            [NotNull] IEnumerable<AnimatorStateMachineScriptUsage> stateMachineUsages)
        {
            var usages = stateUsages.Cast<IScriptUsage>().ToList();
            usages.AddRange(stateMachineUsages);
            return usages;
        }

        private static AnimatorScript? ExtractStateMachineBehaviour([NotNull] AssetDocument document,
                                                                    [NotNull] IPsiSourceFile file)
        {
            var anchorRaw = AssetUtils.GetAnchorFromBuffer(document.Buffer);
            if (!anchorRaw.HasValue) return null;
            var script = document.Document.GetUnityObjectPropertyValue<INode>(UnityYamlConstants.ScriptProperty);
            var guid = (script.ToHierarchyReference(file) as ExternalReference?)?.ExternalAssetGuid;
            return guid != null ? new AnimatorScript(guid.Value, anchorRaw.Value) : (AnimatorScript?) null;
        }

        private void AssertShellLocks()
        {
            myShellLocks.AssertReadAccessAllowed();
        }

        private class PathConsumer<T>
        {
            [NotNull]
            public delegate T Extract([NotNull] IAnimatorScriptUsage usage);

            [NotNull] [ItemNotNull] public readonly List<T> Elements;

            [NotNull] private readonly Extract myExtractFrom;

            public PathConsumer([NotNull] Extract extractor)
            {
                Elements = new List<T>();
                myExtractFrom = extractor;
            }

            public void Consume([NotNull] IAnimatorScriptUsage usage)
            {
                Elements.Add(myExtractFrom(usage));
            }
        }
    }
}