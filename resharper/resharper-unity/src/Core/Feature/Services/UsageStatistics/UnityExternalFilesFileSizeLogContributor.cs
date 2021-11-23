using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.UsageStatistics;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics
{
    [SolutionComponent]
    public class UnityExternalFilesFileSizeLogContributor : IActivityLogContributorSolutionComponent
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly List<Data> myStatistics = new();

        public UnityExternalFilesFileSizeLogContributor(UnitySolutionTracker unitySolutionTracker,
                                                        AssetIndexingSupport assetIndexingSupport)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myAssetIndexingSupport = assetIndexingSupport;
        }

        public void ProcessSolutionStatistics(JObject log)
        {
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return;

            if (myStatistics.Count == 0)
                return;

            // TODO: Perhaps we should rethink this?
            // Does it make more sense to include count, min, max, average instead of all file sizes? This would also
            // reduce the amount of data we cache here

            var stats = new JObject();
            log["uys"] = stats; // Unity Yaml Stats

            // User editable files, e.g. contents of Assets and embedded or local packages
            stats["s"] = JArray.FromObject(GetStatistics(FileType.Scene, true));
            stats["p"] = JArray.FromObject(GetStatistics(FileType.Prefab, true));
            stats["a"] = JArray.FromObject(GetStatistics(FileType.Asset, true));
            stats["ad"] = JArray.FromObject(GetStatistics(FileType.AsmDef, true));
            stats["kba"] = JArray.FromObject(GetStatistics(FileType.KnownBinary, true));
            stats["ebna"] = JArray.FromObject(GetStatistics(FileType.ExcludedByName, true));

            // Referenced, or read-only, non-user editable files
            stats["rs"] = JArray.FromObject(GetStatistics(FileType.Scene, false));
            stats["rp"] = JArray.FromObject(GetStatistics(FileType.Prefab, false));
            stats["ra"] = JArray.FromObject(GetStatistics(FileType.Asset, false));
            stats["rad"] = JArray.FromObject(GetStatistics(FileType.AsmDef, false));
            stats["rkba"] = JArray.FromObject(GetStatistics(FileType.KnownBinary, false));
            stats["rebna"] = JArray.FromObject(GetStatistics(FileType.ExcludedByName, false));

            // There's no need to capture all meta file sizes. Just get a count and an average size
            stats["mfc"] = myStatistics.Count(d => d.FileType == FileType.Meta && d.IsUserEditable);
            stats["rmfc"] = myStatistics.Count(d => d.FileType == FileType.Meta && !d.IsUserEditable);

            // "All meta file average"
            var metaFilesStats = myStatistics.Where(d => d.FileType == FileType.Meta).ToArray();
            if (metaFilesStats.Any())
                stats["amfa"] = metaFilesStats.Average(d => (float) d.Length);

            stats["e"] = myAssetIndexingSupport.IsEnabled.Value;

            // We only get called once per session. Clear the list to free up some memory
            myStatistics.Clear();
        }

        public void AddStatistic(FileType fileType, ulong fileLength, bool isUserEditable)
        {
            myStatistics.Add(new Data(fileType, fileLength, isUserEditable));
        }

        private List<ulong> GetStatistics(FileType type, bool isUserEditable)
        {
            return myStatistics.Where(s => s.FileType == type && s.IsUserEditable == isUserEditable)
                .Select(s => s.Length).ToList();
        }

        public enum FileType
        {
            Asset,
            Prefab,
            Scene,
            AsmDef,
            Meta,

            KnownBinary,
            ExcludedByName
        }

        private struct Data
        {
            public readonly FileType FileType;
            public readonly ulong Length;
            public readonly bool IsUserEditable;

            public Data(FileType fileType, ulong length, bool isUserEditable)
            {
                FileType = fileType;
                Length = length;
                IsUserEditable = isUserEditable;
            }
        }
    }
}