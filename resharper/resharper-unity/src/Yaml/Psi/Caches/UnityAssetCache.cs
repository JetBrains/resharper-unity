using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Collections;
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
        private readonly Dictionary<string, IUnityAssetDataElementContainer> myUnityAssetDataElementContainers = new Dictionary<string, IUnityAssetDataElementContainer>();
        private readonly ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)> myDocumentNumber = new ConcurrentDictionary<IPsiSourceFile, (UnityAssetData, int)>();
        private readonly ConcurrentDictionary<IPsiSourceFile, long> myCurrentTimeStamp = new ConcurrentDictionary<IPsiSourceFile, long>();
        public UnityAssetCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager, IEnumerable<IUnityAssetDataElementContainer> unityAssetDataElementContainers)
            : base(lifetime, persistentIndexManager, new UniversalMarshaller<UnityAssetData>(UnityAssetData.ReadDelegate, UnityAssetData.WriteDelegate))
        {
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
                myUnityAssetDataElementContainers[id].Merge(sourceFile, element);
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
                    var test = new List<int>();
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
                                BuildDocument(result, lifetime, psiSourceFile, documentBuffer);
                            }

                            myDocumentNumber[psiSourceFile] = (result, docId);
                            myCurrentTimeStamp[psiSourceFile] = psiSourceFile.GetAggregatedTimestamp();
                        }

                        test.Add(lexer.TokenEnd);
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

        private void BuildDocument(UnityAssetData data, Lifetime lifetime, IPsiSourceFile sourceFile, IBuffer buffer)
        {
            var assetDocument = new AssetDocument(buffer);
            var results = new LocalList<IUnityAssetDataElement>();
            foreach (var unityAssetDataElementContainer in myUnityAssetDataElementContainers.Values)
            {
                if (!lifetime.IsAlive)
                    throw new OperationCanceledException();
                var result = unityAssetDataElementContainer.Build(lifetime, sourceFile, assetDocument);
                if (result != null)
                    results.Add(result);
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
                myUnityAssetDataElementContainers[id].Drop(sourceFile, element);
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