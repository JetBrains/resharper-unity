using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.UnitTestRunner.JavaScript.Common;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class UnityAssetCache : DeferredCacheBase<UnityAssetData>
    {
        private readonly ILogger myLogger;
        private readonly Dictionary<string, IUnityAssetDataElementContainer> myUnityAssetDataElementContainers = new Dictionary<string, IUnityAssetDataElementContainer>();
        private readonly ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)> myDocumentNumber = new ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)>();
        private readonly ConcurrentDictionary<IPsiSourceFile, long> myCurrentTimeStamp = new ConcurrentDictionary<IPsiSourceFile, long>();
        public UnityAssetCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, IEnumerable<IUnityAssetDataElementContainer> unityAssetDataElementContainers, ILogger logger)
            : base(lifetime, persistentIndexManager, new UniversalMarshaller<UnityAssetData>(UnityAssetData.ReadDelegate, UnityAssetData.WriteDelegate))
        {
            myLogger = logger;
            foreach (var unityAssetDataElementContainer in unityAssetDataElementContainers)
            {
                myUnityAssetDataElementContainers[unityAssetDataElementContainer.Id] = unityAssetDataElementContainer;
            }
        }

        public override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.PsiModule is UnityExternalFilesPsiModule;
        }

        protected override void MergeData(IPsiSourceFile sourceFile, UnityAssetData data)
        {
            foreach (var (id, element) in data.UnityAssetDataElements)
            {
                var container =  myUnityAssetDataElementContainers[id];
                Assertion.Assert(container != null, "container != null");
                try
                {
                    container.Merge(sourceFile, element);
                }
                catch (Exception e)
                {
                    myLogger.Error(e, "An error occurred while merging data in {0}", container.GetType().Name);
                }
            }
        }

        private const int BUFFER_SIZE = 4;
        public override object Build(Lifetime lifetime, IPsiSourceFile psiSourceFile)
        {
            var (result, alreadyBuildDocId) = GetAlreadyBuildDocId(psiSourceFile);
            psiSourceFile.GetLocation().ReadStream(s =>
            {
                using (var sr = new StreamReader(s, Encoding.UTF8, true, BUFFER_SIZE))
                {
                    var buffer = new StreamReaderBuffer(sr, BUFFER_SIZE);
                    var lexer = new UnityYamlLexer(buffer);
                    lexer.Start();

                    var docId = 0;
                    while (lexer.TokenType != null)
                    {
                        if (!lifetime.IsAlive)
                            throw new OperationCanceledException();
                        
                        docId++;

                        if (docId > alreadyBuildDocId)
                        {
                            if (lexer.TokenType == UnityYamlTokenType.DOCUMENT)
                            {
                                var documentBuffer = ProjectedBuffer.Create(buffer, new TextRange(lexer.TokenStart, lexer.TokenEnd));
                                BuildDocument(result, lifetime, psiSourceFile, lexer.TokenStart, documentBuffer);
                            }

                            myDocumentNumber[psiSourceFile] = (result, docId);
                            myCurrentTimeStamp[psiSourceFile] = psiSourceFile.GetAggregatedTimestamp();
                        }

                        buffer.DropFragments();
                        lexer.Advance();
                    }
                    
                    lexer.Advance();
                }
            });
            
            return result;
        }

        private (UnityAssetData, int) GetAlreadyBuildDocId(IPsiSourceFile psiSourceFile)
        {
            if (!myCurrentTimeStamp.TryGetValue(psiSourceFile, out var timeStamp))
                return (new UnityAssetData(), 0);

            if (psiSourceFile.GetAggregatedTimestamp() != timeStamp)
                return (new UnityAssetData(), 0);
            
            return myDocumentNumber[psiSourceFile];
        }

        private void BuildDocument(UnityAssetData data, Lifetime lifetime, IPsiSourceFile sourceFile, int start, IBuffer buffer)
        {
            var assetDocument = new AssetDocument(start, buffer);
            var results = new LocalList<IUnityAssetDataElement>();
            foreach (var unityAssetDataElementContainer in myUnityAssetDataElementContainers.Values)
            {
                if (!lifetime.IsAlive)
                    throw new OperationCanceledException();
                try
                {
                    var result = unityAssetDataElementContainer.Build(lifetime, sourceFile, assetDocument);
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

            foreach (var (id, element) in data.UnityAssetDataElements)
            {
                var container =  myUnityAssetDataElementContainers[id];
                Assertion.Assert(container != null, "container != null");
                try
                {
                    container.Drop(sourceFile, element);
                }
                catch (Exception e)
                {
                    myLogger.Error(e, "An error occurred while dropping data in {0}", container.GetType().Name);
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
        }
    }
}