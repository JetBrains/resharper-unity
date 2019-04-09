using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Diagnostics;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages
{
    // TODO [v.krasnotsvetov] fix test highlighting dumper in sdk
    public class TestHighlighterDumperWithOverridenStages : TestHighlightingDumper
    {
        private int myVersion = 0;

        private class HighlightingInfoWithTimeStamp
        {
            public readonly HighlightingInfo Info;
            public int TimeStamp;
            public HighlightingInfoWithTimeStamp(HighlightingInfo info, int version)
            {
                Info = info;
                TimeStamp = version;
            }
        }
        
        private OneToListMap<Pair<Type, int>, HighlightingInfoWithTimeStamp> myHighlighters =
            new OneToListMap<Pair<Type, int>, HighlightingInfoWithTimeStamp>();

        [NotNull] private readonly Func<IHighlighting, IPsiSourceFile, IContextBoundSettingsStore, bool> myPredicate;
        [CanBeNull] private readonly PsiLanguageType myCompilerIdsLanguage;

        public TestHighlighterDumperWithOverridenStages([NotNull] IPsiSourceFile sourceFile,
            [NotNull] TextWriter writer, [CanBeNull] IReadOnlyCollection<IDaemonStage> stages,
            [NotNull] Func<IHighlighting, IPsiSourceFile, IContextBoundSettingsStore, bool> predicate,
            [CanBeNull] PsiLanguageType compilerIdsLanguage = null)
            : base(sourceFile, writer, stages, predicate, compilerIdsLanguage)
        {
            myPredicate = predicate;
            myCompilerIdsLanguage = compilerIdsLanguage;
        }

        protected override void CommitHighlighters(DaemonCommitContext context)
        {
            lock (this)
            {
                foreach (HighlightingInfo highlightingInfo in context.HighlightingsToAdd)
                {
                    if (myPredicate(highlightingInfo.Highlighting, SourceFile, ContextBoundSettingsStore))
                    {
                        myHighlighters.Add(context.StageId, new HighlightingInfoWithTimeStamp(highlightingInfo, myVersion++));
                    }
                }

                foreach (var id in context.OverridenStage)
                {
                    myHighlighters.RemoveKey(id);
                }
                
                foreach (HighlightingInfoWithTimeStamp highlightingInfoWithTimeStamp in myHighlighters.Values)    
                    Assertion.Assert(highlightingInfoWithTimeStamp.Info != null, "info.Highlighting != null");
            }
        }
        
        public override void Dump()
        {
            Highlighters.Sort(TestHighlightingComparerFixed.Instance);
            var hilightersToDump = Highlighters.Where(h => h.Overlapped != OverlapKind.OVERLAPPED_BY_ERROR).AsList();

            var builder = DocumentRangeUtil.DumpRanges(Document, hilightersToDump.Select(info => info.Range.TextRange), 
                i => "|", i => "|(" + i + ")");

            WriteLine(builder.ToString());
            WriteLine("---------------------------------------------------------");

            var highlightingsManager = HighlightingSettingsManager.Instance;
            var settingsStore = SourceFile.GetLazySettingsStoreWithEditorConfig(Solution);

            for (var i = 0; i < hilightersToDump.Count; i++)
            {
                var info = hilightersToDump[i];

                var idstring = "";
                if (myCompilerIdsLanguage != null)
                {
                    var ids = highlightingsManager.GetCompilerIds(info.Highlighting.GetType(), myCompilerIdsLanguage).ToArray();
                    if (ids.Length > 0)
                    {
                        Array.Sort(ids);
                        idstring = " [" + string.Join(",", ids) + "]";
                    }
                }

                var highlightingTypeSuffix = GetHighlightingTypeSuffix(info);
                var attributeId = info.GetAttributeId(highlightingsManager, SourceFile, Solution, settingsStore);

                if (attributeId == HighlightingAttributeIds.ERROR_ATTRIBUTE)
#pragma warning disable 612
                    attributeId = HighlightingAttributeIds.ERROR_ATTRIBUTE_OLD;
                else if (attributeId == HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE)
                    attributeId = HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE_OLD;
#pragma warning restore 612

                WriteHighlighting(i, attributeId, idstring, info, highlightingTypeSuffix);
            }
        }

        protected class TestHighlightingComparerFixed : IComparer<HighlightingInfo>
        {
            [NotNull] public static readonly TestHighlightingComparer Instance = new TestHighlightingComparer();

            public int Compare([NotNull] HighlightingInfo xInfo, [NotNull] HighlightingInfo yInfo)
            {
                var xHighlighting = xInfo.Highlighting.NotNull("xHighlighting != null");
                var yHighlighting = yInfo.Highlighting.NotNull("yHighlighting != null");

                var result = HighlightingComparer.Instance.Compare(xInfo, yInfo);
                if (result != 0) return result;

                var xToolTip = xHighlighting.ToolTip ?? string.Empty;
                var yToolTip = yHighlighting.ToolTip ?? string.Empty;
                result = string.Compare(xToolTip, yToolTip, StringComparison.Ordinal);
                if (result != 0) return result;

                var xType = xHighlighting.GetType();
                var yType = yHighlighting.GetType();

                return string.Compare(xType.FullName, yType.FullName, StringComparison.Ordinal);
            }
        }
        
        public void CommitAll()
        {
            Highlighters.addAll(myHighlighters.Values.OrderBy(t => t.TimeStamp).Select(t => t.Info));
        }
    }
}