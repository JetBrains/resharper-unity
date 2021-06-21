using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages
{
    [SolutionComponent]
    public class AssetScriptUsagesElementContainer : IScriptUsagesElementContainer
    {
        private readonly IShellLocks myShellLocks;
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        public AssetScriptUsagesElementContainer(IShellLocks shellLocks, MetaFileGuidCache metaFileGuidCache)
        {
            myShellLocks = shellLocks;
            myMetaFileGuidCache = metaFileGuidCache;
        }

        private readonly CountingSet<Guid> myUsagesCount = new CountingSet<Guid>();
        private readonly OneToCompactCountingSet<Guid, IPsiSourceFile> myUsageToSourceFiles = new OneToCompactCountingSet<Guid, IPsiSourceFile>();
        private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer>();

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetScriptUsagesDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return !currentAssetSourceFile.GetLocation().IsControllerFile();
        }

        public object Build(SeldomInterruptChecker checker, IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
        {
            // TODO: deps for other assets
            if (AssetUtils.IsMonoBehaviourDocument(assetDocument.Buffer))
            {
                var anchorRaw = AssetUtils.GetAnchorFromBuffer(assetDocument.Buffer);
                var stripped = AssetUtils.IsStripped(assetDocument.Buffer);
                if (stripped) // we will handle it in prefabs
                    return null;

                if (!anchorRaw.HasValue)
                    return null;

                var anchor = anchorRaw.Value;

                var result = new LocalList<AssetScriptUsage>();
                var entry = assetDocument.Document.GetUnityObjectPropertyValue<INode>(UnityYamlConstants.ScriptProperty);
                var deps = entry.ToHierarchyReference(currentAssetSourceFile);
                if (deps is ExternalReference externalReference)
                    result.Add(new AssetScriptUsage(new LocalReference(currentAssetSourceFile.Ptr().Id, anchor), externalReference));

                return result;
            }

            return null;
        }

        public void Drop(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElement unityAssetDataElement)
        {
            var dataElement = unityAssetDataElement as AssetScriptUsagesDataElement;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                if (!(assetUsagePointer is AssetScriptUsage assetScriptUsages)) continue;
                var guid = assetScriptUsages.UsageTarget.ExternalAssetGuid;
                myUsagesCount.Remove(guid);
                myUsageToSourceFiles.Remove(guid, currentAssetSourceFile);
            }

            myPointers.Remove(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;
            var dataElement = unityAssetDataElement as AssetScriptUsagesDataElement;
            foreach (var assetUsagePointer in dataElement.EnumerateAssetUsages())
            {
                if (!(assetUsagePointer is AssetScriptUsage assetScriptUsages)) continue;
                var guid = assetScriptUsages.UsageTarget.ExternalAssetGuid;
                myUsagesCount.Add(guid);
                myUsageToSourceFiles.Add(guid, currentAssetSourceFile);
            }
        }

        public int GetScriptUsagesCount(IClassLikeDeclaration classLikeDeclaration, out bool estimatedResult)
        {
            myShellLocks.AssertReadAccessAllowed();

            // TODO : prefabs
            estimatedResult = false;

            var sourceFile = classLikeDeclaration.GetSourceFile();
            if (sourceFile == null)
                return 0;

            var declaredElement = classLikeDeclaration.DeclaredElement;
            if (declaredElement == null)
                return 0;

            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            if (guid == null)
                return 0;

            return myUsagesCount.GetCount(guid.Value);
        }

        public string Id => nameof(AssetScriptUsagesElementContainer);
        public int Order => 0;
        public void Invalidate()
        {
            myUsageToSourceFiles.Clear();
            myUsagesCount.Clear();
            myPointers.Clear();
        }

        public IEnumerable<IScriptUsage> GetScriptUsagesFor(IPsiSourceFile sourceFile, ITypeElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            var element = myPointers[sourceFile].GetElement(sourceFile, Id) as AssetScriptUsagesDataElement;
            if (element == null) return Enumerable.Empty<IScriptUsage>();
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            if (guid == null) return Enumerable.Empty<IScriptUsage>();
            return element
                .EnumerateAssetUsages()
                .Where(t => t.UsageTarget.ExternalAssetGuid == guid)
                .Cast<IScriptUsage>();
        }

        public LocalList<IPsiSourceFile> GetPossibleFilesWithScriptUsages(IClass scriptClass)
        {
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, scriptClass);
            if (guid == null)
                return new LocalList<IPsiSourceFile>();

            var result = new LocalList<IPsiSourceFile>();
            foreach (var assetUsage in myUsageToSourceFiles.GetValues(guid.Value))
            {
                result.Add(assetUsage);
            }

            return result;
        }
    }
}