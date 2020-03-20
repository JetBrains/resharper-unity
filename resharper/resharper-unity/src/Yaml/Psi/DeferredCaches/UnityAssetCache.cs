using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Interning;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
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
    public class UnityAssetCache : DeferredCacheBase<UnityAssetData>
    {
        private readonly AssetDocumentHierarchyElementContainer myHierarchyElementContainer;
        private readonly DocumentToProjectFileMappingStorage myDocumentToProjectFileMappingStorage;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly PrefabImportCache myPrefabImportCache;
        private readonly UnityInterningCache myInterningCache;
        private readonly IShellLocks myShellLocks;
        private readonly ILogger myLogger;
        private readonly List<IUnityAssetDataElementContainer> myOrderedContainers;
        private readonly List<IUnityAssetDataElementContainer> myOrderedIncreasingContainers;
        private readonly ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)> myDocumentNumber = new ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)>();
        private readonly ConcurrentDictionary<IPsiSourceFile, long> myCurrentTimeStamp = new ConcurrentDictionary<IPsiSourceFile, long>();
        public UnityAssetCache(Lifetime lifetime, DocumentToProjectFileMappingStorage documentToProjectFileMappingStorage, AssetIndexingSupport assetIndexingSupport,
            PrefabImportCache prefabImportCache, IPersistentIndexManager persistentIndexManager, IEnumerable<IUnityAssetDataElementContainer> unityAssetDataElementContainers,
            UnityInterningCache interningCache, IShellLocks shellLocks, ILogger logger)
            : base(lifetime, persistentIndexManager, new UniversalMarshaller<UnityAssetData>(UnityAssetData.ReadDelegate, UnityAssetData.WriteDelegate))
        {
            myDocumentToProjectFileMappingStorage = documentToProjectFileMappingStorage;
            myAssetIndexingSupport = assetIndexingSupport;
            myPrefabImportCache = prefabImportCache;
            myInterningCache = interningCache;
            myShellLocks = shellLocks;
            myLogger = logger;
            myOrderedContainers = unityAssetDataElementContainers.OrderByDescending(t => t.Order).ToList();
            myOrderedIncreasingContainers = myOrderedContainers.OrderBy(t => t.Order).ToList();

            myHierarchyElementContainer = myOrderedContainers.First(t => t is AssetDocumentHierarchyElementContainer) as AssetDocumentHierarchyElementContainer;
            
            Map.Cache = new DirectMappedCache<IPsiSourceFile, UnityAssetData>(10);
        }

        public override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            if (!myAssetIndexingSupport.IsEnabled.Value)
                return false;
            
            return sourceFile.PsiModule is UnityExternalFilesPsiModule;
        }

        protected override void MergeData(IPsiSourceFile sourceFile, UnityAssetData data)
        {
            myDocumentNumber.TryRemove(sourceFile, out _);
            myCurrentTimeStamp.TryRemove(sourceFile, out _);

            if (!data.UnityAssetDataElements.TryGetValue(myHierarchyElementContainer.Id, out var hierarchyElement))
                return;
            
            foreach (var container in myOrderedContainers)
            {
                if (data.UnityAssetDataElements.TryGetValue(container.Id, out var element))
                {
                    Assertion.Assert(container != null, "container != null");
                    try
                    {
                        container.Merge(sourceFile, hierarchyElement as AssetDocumentHierarchyElement,
                            new UnityAssetDataElementPointer(() =>
                            {
                                myShellLocks.AssertReadAccessAllowed();
                                var restoredElement =  Map[sourceFile].UnityAssetDataElements[container.Id];
                                if (restoredElement is AssetDocumentHierarchyElement hierarchy)
                                    hierarchy.RestoreHierarchy(myInterningCache);
                                
                                return restoredElement;
                            }), element);
                    }
                    catch (Exception e)
                    {
                        myLogger.Error(e, "An error occurred while merging data in {0}", container.GetType().Name);
                    }
                }
            }
        }

        private const int BUFFER_SIZE = 4096;
        public override object Build(IPsiSourceFile psiSourceFile)
        {
            var checker = new SeldomInterruptChecker();
            if (!myAssetIndexingSupport.IsEnabled.Value)
                return null;
                
            if (!psiSourceFile.GetLocation().SniffYamlHeader())
                return new UnityAssetData();
            
            var (result, alreadyBuildDocId) = GetAlreadyBuildDocId(psiSourceFile);
            EnumerateDocuments(psiSourceFile, buffer =>
            {
                var lexer = new UnityYamlLexer(buffer);
                lexer.Start();

                var docId = 0;
                while (lexer.TokenType != null)
                {
                    checker.CheckForInterrupt();
                        
                    docId++;

                    if (docId > alreadyBuildDocId)
                    {
                        if (lexer.TokenType == UnityYamlTokenType.DOCUMENT)
                        {
                            var documentBuffer = ProjectedBuffer.Create(buffer, new TextRange(lexer.TokenStart, lexer.TokenEnd));
                            BuildDocument(result, checker, psiSourceFile, lexer.TokenStart, documentBuffer);
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
                return (new UnityAssetData(), 0);

            if (psiSourceFile.GetAggregatedTimestamp() != timeStamp)
                return (new UnityAssetData(), 0);
            
            return myDocumentNumber[psiSourceFile];
        }

        private void BuildDocument(UnityAssetData data, SeldomInterruptChecker checker, IPsiSourceFile sourceFile, int start, IBuffer buffer)
        {
            var assetDocument = new AssetDocument(start, buffer);
            var results = new LocalList<IUnityAssetDataElement>();
            foreach (var unityAssetDataElementContainer in myOrderedContainers)
            {
                checker.CheckForInterrupt();
                
                try
                {
                    var result = unityAssetDataElementContainer.Build(checker, sourceFile, assetDocument);
                    if (result != null)
                        results.Add(result);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    myLogger.Error(e, "An error occured while building document: {0}", unityAssetDataElementContainer.GetType().Name);
                }
            }

            foreach (var result in results)
            {
                data.AddDataElement(result);
            }
        }

        public override void DropData(IPsiSourceFile sourceFile, UnityAssetData data)
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
                        myLogger.Error(e, "An error occurred while dropping data in {0}", container.GetType().Name);
                    }
                }
            }
        }

        public override void MergeLoadedData()
        {
            foreach (var (sourceFile, unityAssetData) in Map)
            {
                MergeData(sourceFile, unityAssetData);
            }

        }

        public override void InvalidateData()
        {
            foreach (var increasingContainer in myOrderedIncreasingContainers)
            {
                increasingContainer.Invalidate();
            }

            myPrefabImportCache.Invalidate();
        }
    }
}