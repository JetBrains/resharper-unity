using System;
using System.Collections.Generic;
using System.IO;
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
        private OneToListMap<Pair<Type, int>, HighlightingInfo> myHighlighters =
            new OneToListMap<Pair<Type, int>, HighlightingInfo>();

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
                        myHighlighters.Add(context.StageId, highlightingInfo);
                    }
                }

                foreach (var id in context.OverridenStage)
                {
                    myHighlighters.RemoveKey(id);
                }
                
                foreach (HighlightingInfo highlighter in myHighlighters.Values)
                    Assertion.Assert(highlighter.Highlighting != null, "info.Highlighting != null");
            }
        }

        public void CommitAll()
        {
            Highlighters.addAll(myHighlighters.Values);
        }
    }
}