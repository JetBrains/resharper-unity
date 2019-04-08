using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
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

        public TestHighlighterDumperWithOverridenStages([NotNull] IPsiSourceFile sourceFile,
            [NotNull] TextWriter writer, [CanBeNull] IReadOnlyCollection<IDaemonStage> stages,
            [NotNull] Func<IHighlighting, IPsiSourceFile, IContextBoundSettingsStore, bool> predicate,
            [CanBeNull] PsiLanguageType compilerIdsLanguage = null)
            : base(sourceFile, writer, stages, predicate, compilerIdsLanguage)
        {
            myPredicate = predicate;
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

        public void CommitAll()
        {
            Highlighters.addAll(myHighlighters.Values.OrderBy(t => t.TimeStamp).Select(t => t.Info));
        }
    }
}