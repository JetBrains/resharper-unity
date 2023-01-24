using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
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

        private readonly CountingSet<Guid> myUsagesCount = new();
        private readonly OneToCompactCountingSet<Guid, IPsiSourceFile> myUsageToSourceFiles = new();
        private readonly Dictionary<IPsiSourceFile, IUnityAssetDataElementPointer> myPointers = new();

        public IUnityAssetDataElement CreateDataElement(IPsiSourceFile sourceFile)
        {
            return new AssetScriptUsagesDataElement();
        }

        public bool IsApplicable(IPsiSourceFile currentAssetSourceFile)
        {
            return !currentAssetSourceFile.IsController();
        }

        public object Build(IPsiSourceFile currentAssetSourceFile, AssetDocument assetDocument)
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
            var dataElement = (AssetScriptUsagesDataElement)unityAssetDataElement;
            foreach (var assetScriptUsages in dataElement.EnumerateAssetUsages())
            {
                var guid = assetScriptUsages.UsageTarget.ExternalAssetGuid;
                myUsagesCount.Remove(guid);
                myUsageToSourceFiles.Remove(guid, currentAssetSourceFile);
            }

            myPointers.Remove(currentAssetSourceFile);
        }

        public void Merge(IPsiSourceFile currentAssetSourceFile, AssetDocumentHierarchyElement assetDocumentHierarchyElement, IUnityAssetDataElementPointer unityAssetDataElementPointer, IUnityAssetDataElement unityAssetDataElement)
        {
            var assetScriptUsagesDataElement = (AssetScriptUsagesDataElement)unityAssetDataElement;
            myPointers[currentAssetSourceFile] = unityAssetDataElementPointer;
            foreach (var assetUsagePointer in assetScriptUsagesDataElement.EnumerateAssetUsages())
            {
                var guid = assetUsagePointer.UsageTarget.ExternalAssetGuid;
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
        
        public IEnumerable<IScriptUsage> GetScriptUsagesFor(ITypeElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            var guid = AssetUtils.GetGuidFor(myMetaFileGuidCache, declaredElement);
            return myPointers.SelectMany(pointer =>
            {
                var element = (AssetScriptUsagesDataElement)pointer.Value.GetElement(pointer.Key, Id);
                return element.EnumerateAssetUsages()
                    .Where(t => t.UsageTarget.ExternalAssetGuid == guid)
                    .Cast<IScriptUsage>();
            });
        }

        public IEnumerable<IScriptUsage> GetScriptUsagesFor(IPsiSourceFile sourceFile, ITypeElement declaredElement)
        {
            myShellLocks.AssertReadAccessAllowed();
            if (!IsApplicable(sourceFile)) return Enumerable.Empty<IScriptUsage>();
            if (myPointers[sourceFile].GetElement(sourceFile, Id) is not AssetScriptUsagesDataElement element) return Enumerable.Empty<IScriptUsage>();
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