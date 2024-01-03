using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.DocumentManagers.impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Caches;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    [SolutionComponent]
    public class UnityAssetsCache : DeferredCacheBase<UnityAssetData> , IUnityAssetDataElementPointer
    {
        private readonly AssetDocumentHierarchyElementContainer myHierarchyElementContainer;
        private readonly DocumentToProjectFileMappingStorage myDocumentToProjectFileMappingStorage;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly PrefabImportCache myPrefabImportCache;
        private readonly IShellLocks myShellLocks;
        private readonly List<IUnityAssetDataElementContainer> myOrderedContainers;
        private readonly List<IUnityAssetDataElementContainer> myOrderedIncreasingContainers;
        private readonly ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)> myDocumentNumber = new ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)>();
        private readonly ConcurrentDictionary<IPsiSourceFile, long> myCurrentTimeStamp = new ConcurrentDictionary<IPsiSourceFile, long>();
        public UnityAssetsCache(Lifetime lifetime, DocumentToProjectFileMappingStorage documentToProjectFileMappingStorage, AssetIndexingSupport assetIndexingSupport,
            PrefabImportCache prefabImportCache, IPersistentIndexManager persistentIndexManager, IEnumerable<IUnityAssetDataElementContainer> unityAssetDataElementContainers,
            IShellLocks shellLocks, ILogger logger)
            : base(lifetime, persistentIndexManager, new UniversalMarshaller<UnityAssetData>(UnityAssetData.ReadDelegate, UnityAssetData.WriteDelegate), logger)
        {
            myDocumentToProjectFileMappingStorage = documentToProjectFileMappingStorage;
            myAssetIndexingSupport = assetIndexingSupport;
            myPrefabImportCache = prefabImportCache;
            myShellLocks = shellLocks;
            myOrderedContainers = unityAssetDataElementContainers.OrderByDescending(t => t.Order).ToList();
            myOrderedIncreasingContainers = myOrderedContainers.OrderBy(t => t.Order).ToList();

            myHierarchyElementContainer = myOrderedContainers.First(t => t is AssetDocumentHierarchyElementContainer) as AssetDocumentHierarchyElementContainer;
            
            Map.Cache = new DirectMappedCache<IPsiSourceFile, UnityAssetData>(10);
        }

        public override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            if (!myAssetIndexingSupport.IsEnabled.Value)
                return false;

            if (sourceFile.PsiModule is UnityExternalFilesPsiModule && sourceFile.IsYamlDataFile())
                return ValidateSize(sourceFile);
            
            return false;
        }

        private const int AnimMaxSize = 50 * 1024 * 1024; //50mb
        private bool ValidateSize(IPsiSourceFile sourceFile)
        {
            var location = sourceFile.GetLocation();
            if (!location.IsAnim() && !location.IsController())
                return true;
            
            return Logger.CatchSilent(() =>
            {
                return location.GetFileLength() <= AnimMaxSize;
            });
        }

        protected override void MergeData(IPsiSourceFile sourceFile, UnityAssetData data)
        {
            myDocumentNumber.TryRemove(sourceFile, out _);
            myCurrentTimeStamp.TryRemove(sourceFile, out _);

            if (!data.UnityAssetDataElements.TryGetValue(myHierarchyElementContainer.Id, out var hierarchyElement))
                return;
            
            foreach (var container in myOrderedContainers)
            {
                if (!container.IsApplicable(sourceFile)) continue;
                
                if (data.UnityAssetDataElements.TryGetValue(container.Id, out var element))
                {
                    Assertion.Assert(container != null, "container != null");
                    try
                    {
                        container.Merge(sourceFile, hierarchyElement as AssetDocumentHierarchyElement,this, element);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "An error occurred while merging data in {0}", container.GetType().Name);
                    }
                }
            }
        }

        private const int BUFFER_SIZE = 4096;
        public override object Build(IPsiSourceFile psiSourceFile)
        {
            if (!myAssetIndexingSupport.IsEnabled.Value)
                return null;
                
            if (!psiSourceFile.GetLocation().SniffYamlHeader())
                return new UnityAssetData(psiSourceFile, EmptyList<IUnityAssetDataElementContainer>.Enumerable);
            
            var (result, alreadyBuildDocId) = GetAlreadyBuildDocId(psiSourceFile);
            EnumerateDocuments(psiSourceFile, buffer =>
            {
                var lexer = new UnityYamlLexer(buffer);
                lexer.Start();

                var docId = 0;
                while (lexer.TokenType != null)
                {
                    Interruption.Current.CheckAndThrow();
                        
                    docId++;

                    if (docId > alreadyBuildDocId)
                    {
                        if (lexer.TokenType == UnityYamlTokenType.DOCUMENT)
                        {
                            var documentBuffer = ProjectedBuffer.Create(buffer, new TextRange(lexer.TokenStart, lexer.TokenEnd));
                            BuildDocument(result, psiSourceFile, lexer.TokenStart, documentBuffer);
                        }

                        myDocumentNumber[psiSourceFile] = (result, docId);
                        myCurrentTimeStamp[psiSourceFile] = psiSourceFile.GetAggregatedTimestamp();
                    }

                    if (buffer is StreamReaderBuffer streamReaderBuffer)
                    {
                        streamReaderBuffer.DropFragments();
                    }

                    lexer.Advance();
                }
            });
            
            return result;
        }

        private void EnumerateDocuments(IPsiSourceFile sourceFile, Action<IBuffer> bufferProcessor)
        {
            var existingDocument = myDocumentToProjectFileMappingStorage.TryGetDocumentByPath(sourceFile.GetLocation());
            if (existingDocument != null)
            {
                bufferProcessor(existingDocument.Buffer);
            }
            else
            {
                sourceFile.GetLocation().ReadStream(s =>
                {
                    using (var sr = new StreamReader(s, Encoding.UTF8, true, BUFFER_SIZE))
                    {
                        var buffer = new StreamReaderBuffer(sr, BUFFER_SIZE);
                        bufferProcessor(buffer);
                    }
                });
            }
        }

        private (UnityAssetData, int) GetAlreadyBuildDocId(IPsiSourceFile psiSourceFile)
        {
            if (!myCurrentTimeStamp.TryGetValue(psiSourceFile, out var timeStamp))
                return (new UnityAssetData(psiSourceFile, myOrderedContainers), 0);

            if (psiSourceFile.GetAggregatedTimestamp() != timeStamp)
                return (new UnityAssetData(psiSourceFile, myOrderedContainers), 0);
            
            return myDocumentNumber[psiSourceFile];
        }

        private void BuildDocument(UnityAssetData data, IPsiSourceFile assetSourceFile, int start, IBuffer buffer)
        {
            var assetDocument = new AssetDocument(start, buffer, null);
            var results = new LocalList<(string, object)>();
            foreach (var unityAssetDataElementContainer in myOrderedContainers)
            {
                Interruption.Current.CheckAndThrow();
                
                try
                {
                    if (!unityAssetDataElementContainer.IsApplicable(assetSourceFile)) continue;
                    var result = unityAssetDataElementContainer.Build(assetSourceFile, assetDocument);
                    if (result is IHierarchyElement hierarchyElement)
                        assetDocument = assetDocument.WithHiererchyElement(hierarchyElement);

                    if (result != null)
                        results.Add((unityAssetDataElementContainer.Id, result));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"An error occured while building document: {unityAssetDataElementContainer.GetType().Name}");
                }
            }

            foreach (var result in results)
            {
                data.UnityAssetDataElements[result.Item1].AddData(result.Item2);
            }
        }

        protected override void DropData(IPsiSourceFile sourceFile, UnityAssetData data)
        {
            myDocumentNumber.TryRemove(sourceFile, out _);
            myCurrentTimeStamp.TryRemove(sourceFile, out _);

            if (!data.UnityAssetDataElements.TryGetValue(myHierarchyElementContainer.Id, out var hierarchyElement))
                return;
            
            foreach (var container in myOrderedIncreasingContainers)
            {
                if (data.UnityAssetDataElements.TryGetValue(container.Id, out var element))
                {
                    Assertion.Assert(container != null, "container != null");
                    try
                    {
                        container.Drop(sourceFile, hierarchyElement as AssetDocumentHierarchyElement, element);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "An error occurred while dropping data in {0}", container.GetType().Name);
                    }
                }
            }
        }

        public override void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change)
        {
            // TODO : temp solution
            if (sourceFile is UnityExternalPsiSourceFile unityYamlExternalPsiSourceFile)
            {
                unityYamlExternalPsiSourceFile.MarkDocumentModified();
            }

            base.OnDocumentChange(sourceFile, change);
        }

        protected override void InvalidateData()
        {
            foreach (var increasingContainer in myOrderedIncreasingContainers)
            {
                increasingContainer.Invalidate();
            }

            myPrefabImportCache.Invalidate();
        }

        public IUnityAssetDataElement GetElement(IPsiSourceFile assetSourceFile, string containerId)
        {
            myShellLocks.AssertReadAccessAllowed();
            var restoredElement = Map[assetSourceFile].UnityAssetDataElements[containerId];
            if (restoredElement is AssetDocumentHierarchyElement hierarchy)
                hierarchy.RestoreHierarchy(myHierarchyElementContainer, assetSourceFile);
            
            return restoredElement;

        }
    }
}