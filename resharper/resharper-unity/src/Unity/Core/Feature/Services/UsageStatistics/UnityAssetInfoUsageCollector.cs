using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Collections.Synchronized;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;
using JetBrains.UsageStatistics.FUS.Utils;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics
{
    [SolutionComponent]
    public class UnityAssetInfoCollector : SolutionUsagesCollector
    {
        private EventLogGroup myGroup;
        
        private readonly EventId2<long, bool> myMetaFileAverage;
        private readonly EventId1<long> myFilesAverage;
        private readonly EventId3<FileType,long, bool> myFileSizeMax;
        private readonly EventId1<int> myMetaCount;
        private readonly EventId1<int> myAssetCount;

        private IViewableProperty<bool> IsReady { get; } = new ViewableProperty<bool>(false);
        private IViewableProperty<bool> InitialUpdateFinished { get; } = new ViewableProperty<bool>(false);

        public UnityAssetInfoCollector(Lifetime lifetime, PackageManager packageManager, FeatureUsageLogger featureUsageLogger)
        {
            myGroup = new EventLogGroup("dotnet.unity.assets", "Unity Asset Information", 2, featureUsageLogger);
            
            myMetaFileAverage = myGroup.RegisterEvent("metaAverage", "Meta Files Average", EventFields.Long("average", "Average (bytes)"), EventFields.Boolean("isReadonly", "IsReadonly"));
            myFilesAverage = myGroup.RegisterEvent("assetAverage", "All Asset Files Average", EventFields.Long("average", "Average (mb)"));

            myFileSizeMax = myGroup.RegisterEvent("fileSizeMax", "Max file size (bytes) for each file type", 
                EventFields.Enum<FileType>("type", "File Type"), 
                EventFields.Long("size", "Max size (bytes)"),
                EventFields.Boolean("isReadonly", "IsReadonly")
                );

            myMetaCount = myGroup.RegisterEvent("metaCount", "Meta Files Count", EventFields.Int("count", "Count"));
            myAssetCount = myGroup.RegisterEvent("assetCount", "Asset Files Count", EventFields.Int("count", "Count"));

            InitialUpdateFinished.AdviseUntil(lifetime, v =>
            {
                if (v)
                {
                    packageManager.IsInitialUpdateFinished.AdviseUntil(lifetime, v2 =>
                    {
                        if (v2)
                        {
                            IsReady.Value = true;
                            return true;
                        }

                        return false;
                    });
                    return true;
                }

                return false;
            });
        }
        
        public override EventLogGroup GetGroup()
        {
            return myGroup;
        }

        // cache answer
        private ISet<MetricEvent> myEvents = null;
        public override Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
        {
            if (myEvents != null)
                return Task.FromResult(myEvents);
            
            var tcs = lifetime.CreateTaskCompletionSource<ISet<MetricEvent>>(TaskCreationOptions.RunContinuationsAsynchronously);

            IsReady.AdviseUntil(lifetime, v =>
            {
                if (v)
                {
                    lifetime.StartBackground(() =>
                    {
                        var hashSet = new HashSet<MetricEvent>();

                        long totalSize = 0;
                        long metaSize = 0;
                        long readonlyMetaSize = 0;

                        var maxFileSizes = new Dictionary<string, long>();
                        var readonlyMaxFileSizes = new Dictionary<string, long>();

                        int metaCount = 0;
                        int assetCount = 0;
                        
                        foreach (var statistic in myStatistics)
                        {
                            if (statistic.FileType == FileType.Meta)
                            {
                                if (statistic.IsUserEditable)
                                {
                                    metaSize += statistic.Length;
                                }
                                else
                                {
                                    readonlyMetaSize += statistic.Length;
                                }


                                metaCount++;
                            }
                            else
                            {
                                if (statistic.IsUserEditable)
                                {
                                    if (maxFileSizes.TryGetValue(statistic.FileType.ToString()) < statistic.Length)
                                        maxFileSizes[statistic.FileType.ToString()] = statistic.Length;    
                                }
                                else
                                {
                                    if (readonlyMaxFileSizes.TryGetValue(statistic.FileType.ToString()) < statistic.Length)
                                        readonlyMaxFileSizes[statistic.FileType.ToString()] = statistic.Length;    
                                }
                                    
                                totalSize += statistic.Length;
                                assetCount++;
                            }
                        }
                        
                        hashSet.Add(myMetaFileAverage.Metric(StatisticsUtil.GetNextPowerOfTwo(metaSize), false));
                        hashSet.Add(myMetaFileAverage.Metric(StatisticsUtil.GetNextPowerOfTwo(readonlyMetaSize), true));
                        hashSet.Add(myFilesAverage.Metric(StatisticsUtil.GetNextPowerOfTwo(totalSize / 1024 / 1024)));

                        foreach (var fileType in maxFileSizes.Keys)
                        {
                            hashSet.Add(myFileSizeMax.Metric((FileType)Enum.Parse(typeof(FileType), fileType), 
                                StatisticsUtil.GetNextPowerOfTwo(maxFileSizes[fileType]), false));
                        }
                        
                        foreach (var fileType in readonlyMaxFileSizes.Keys)
                        {
                            hashSet.Add(myFileSizeMax.Metric((FileType)Enum.Parse(typeof(FileType), fileType), 
                                StatisticsUtil.GetNextPowerOfTwo(readonlyMaxFileSizes[fileType]), true));
                        }
                        
                        hashSet.Add(myMetaCount.Metric(StatisticsUtil.GetNextPowerOfTwo(metaCount)));
                        hashSet.Add(myAssetCount.Metric(StatisticsUtil.GetNextPowerOfTwo(assetCount)));

                        myEvents = hashSet;
                        myStatistics = null;

                        tcs.TrySetResult(hashSet);
                    });

                    return true;
                }

                return false;
            });
            
            return tcs.Task;
        }

        public struct AssetData
        {
            public readonly FileType FileType;
            public readonly ulong Length;
            public readonly bool IsUserEditable;

            public AssetData(FileType fileType, ulong length, bool isUserEditable)
            {
                FileType = fileType;
                Length = length;
                IsUserEditable = isUserEditable;
            }
        }
        
        public enum FileType
        {
            Asset,
            Prefab,
            Scene,
            AsmDef,
            AsmRef,
            Meta,
            InputActions,
            Anim,
            Controller,

            KnownBinary,
            ExcludedByName
        }
        
        private struct Data
        {
            public readonly FileType FileType;
            public readonly long Length;
            public readonly bool IsUserEditable;

            public Data(FileType fileType, long length, bool isUserEditable)
            {
                FileType = fileType;
                Length = length;
                IsUserEditable = isUserEditable;
            }
        }

        public void FinishInitialUpdate()
        {
            InitialUpdateFinished.Value = true;
        }

        // called from MT
        private SynchronizedList<Data> myStatistics = new();
        
        public void AddStatistic(FileType fileType, long externalFileLength, bool externalFileIsUserEditable)
        {
            if (IsReady.Value)
                return;
            
            myStatistics.Add(new Data(fileType, externalFileLength, externalFileIsUserEditable));
        }
    }
}