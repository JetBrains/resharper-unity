using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls.BulbMenu;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions.Scoped;
using JetBrains.ReSharper.Feature.Services.Naming;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.FeaturesTestFramework.BulbMenu;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Intentions.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    /// <summary>
    /// Copy-pasted from QuickFixAvailabilityTestBase and added swea + global warnings
    /// may be best idea to add virtual method, but this must be considered
    /// </summary>
    public class QuickFixAfterSwaAvailabilityTestBase : BaseTestWithTextControl
    {
        public override FileSystemPath ProjectBasePath
        {
            get { return EnsureAndCleanupTestSolutionFolder(); }
        }

        protected virtual bool UseUnderCaretHighlighter
        {
            get { return false; }
        }

        protected virtual bool HighlightingPredicate([NotNull] IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            var settingsManager = HighlightingSettingsManager.Instance;
            if (settingsManager.GetSeverity(highlighting, psiSourceFile, Solution, boundSettingsStore) ==
                Severity.INFO &&
                settingsManager.GetHighlightingAttribute(highlighting).OverlapResolve == OverlapResolveKind.NONE)
                return false;

            if (highlighting is InconsistentNamingWarningBase)
                return true;

            var highlightingTestBehaviour = highlighting as IHighlightingTestBehaviour;
            if (highlightingTestBehaviour != null && highlightingTestBehaviour.IsSuppressed)
                return false;

            return true;
        }

        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var position = GetCaretPosition();

            IProjectFile item;
            if (position == null)
            {
                item = testProject.GetSubItemsRecursively().OfType<IProjectFile>().First("No project file");
            }
            else
            {
                item = (IProjectFile) testProject.FindProjectItemsByLocation(position.FileName)
                    .Single("No project file", "More than one project file");
            }

            var swea = SolutionAnalysisService.GetInstance(Solution);
            Assert.IsTrue(item.Kind == ProjectItemKind.PHYSICAL_FILE);
            using (TestPresentationMap.Cookie())
            using (swea.RunAnalysisCookie())
            {
                using (var definition = Lifetime.Define(lifetime))
                {
                    var boundstore = ChangeSettingsTemporarily(definition.Lifetime).BoundStore;
                    boundstore.SetValue(HighlightingSettingsAccessor.CalculateUnusedTypeMembers, InplaceUsageAnalysis);

                    ITextControl textControl = position != null
                        ? OpenTextControl(definition.Lifetime, position)
                        : OpenTextControl(definition.Lifetime, item);
                    {
                        swea.ReanalyzeFile(item.ToSourceFile());
                        using (CompilationContextCookie.GetOrCreate(textControl.GetContext(Solution)))
                        {
                            ExecuteWithGold(item, writer =>
                            {
                                var highlightings = GetHighlightings(textControl);

                                DumpQuickFixesAvailability(writer, highlightings, textControl, Solution);
                            });
                        }
                    }
                }
            }
        }

        protected virtual bool InplaceUsageAnalysis => false;

        [NotNull, MustUseReturnValue]
        private IEnumerable<HighlightingInfo> GetHighlightings([NotNull] ITextControl textControl)
        {
            if (UseUnderCaretHighlighter)
            {
                var highlightingsUnderCaretProvider = Solution.GetComponent<HighlightingsUnderCaretProvider>();
                var highlighter = highlightingsUnderCaretProvider.GetHighlighterUnderCaret(textControl)
                    .NotNull("highlighter != null");
                var analysisRange = new DocumentRange(textControl.Caret.DocumentOffset());

                return highlighter.GetHighlightingsForRange(analysisRange);
            }

            var sourceFile = textControl.Document.GetPsiSourceFile(Solution).NotNull();
            var daemonProcessMock = new DaemonProcessMock(sourceFile);
            daemonProcessMock.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
            
            // next line is only difference with QuickFixAvailabilityTestBase
            // CGTD may be we should add virtual method to allow tests with swea?
            daemonProcessMock.DoHighlighting(DaemonProcessKind.GLOBAL_WARNINGS);

            return daemonProcessMock.Highlightings;
        }

        private bool FilterEmptyHighlighting()
        {
            return myQuickFixPredicate != null;
        }

        protected void DoNamedTest(Func<IQuickFix, bool> quickFixPredicate)
        {
            var old = myQuickFixPredicate;
            try
            {
                myQuickFixPredicate = quickFixPredicate;
                DoNamedTest();
            }
            finally
            {
                myQuickFixPredicate = old;
            }
        }

        private Func<IQuickFix, bool> myQuickFixPredicate;

        private bool QuickFixPredicate(IQuickFix quickFix)
        {
            return myQuickFixPredicate == null || myQuickFixPredicate(quickFix);
        }

        private void DumpQuickFixesAvailability(
            [NotNull] TextWriter writer,
            [NotNull] IEnumerable<HighlightingInfo> highlightings,
            [NotNull] ITextControl textControl,
            [NotNull] ISolution solution)
        {
            var sourceFile = textControl.Document.GetPsiSourceFile(solution).NotNull("sourceFile != null");
            var quickFixesTable = solution.GetComponent<IQuickFixes>();
            var quickFixesProvider = solution.GetComponent<QuickFixesProvider>();
            var locks = solution.Locks;

            var boundSettingsStore = sourceFile.GetLazySettingsStoreWithEditorConfig(Solution);
            var filteredHighlightings = highlightings.Where(
                info => info.Highlighting.IsValid() &&
                        info.Overlapped != OverlapKind.OVERLAPPED_BY_ERROR &&
                        HighlightingPredicate(info.Highlighting, sourceFile, boundSettingsStore));

            IList<HighlightingInfo> sourtedHighlightings;
            IList<TestFrameworkUtil.Position> sortedHighlightingPositions;

            TestFrameworkUtil.SortItems(
                filteredHighlightings,
                HighlightingComparer.Instance,
                info => info.Range,
                out sourtedHighlightings,
                out sortedHighlightingPositions);

            var text = sourceFile.Document.Buffer;
            var i = -1;

            TestFrameworkUtil.DumpReferencePositions(writer, text, sortedHighlightingPositions);

            foreach (var info in sourtedHighlightings)
            {
                ICollection<IQuickFix> quickFixes;
                using (ReadLockCookie.Create())
                {
                    var sourceHighlighting = info.Highlighting;
                    if (sourceHighlighting is IDelegatingHighlighting delegatingHighlighting)
                        sourceHighlighting = delegatingHighlighting.DelegatesTo;

                    quickFixes = quickFixesTable.CreateAvailableQuickFixes(sourceHighlighting, new UserDataHolder());
                    quickFixes = quickFixes.Where(QuickFixPredicate).ToList();
                }

                i++;

                if (FilterEmptyHighlighting() && quickFixes.IsEmpty()) continue;

                writer.WriteLine("{0}: {1}", i, info.Highlighting.ToolTip);

                var scopedIntentionsManager = solution.GetComponent<ScopedIntentionsManager>();
                var bulbItems = quickFixes
                    .SelectMany(
                        quickFix => scopedIntentionsManager.GetScopedIntentions(quickFix, solution, textControl))
                    .Select(intentionAction => quickFixesProvider.CreateBulbMenuItem(intentionAction, textControl,
                        info.Highlighting, sourceFile, boundSettingsStore))
                    .ToList();

                if (bulbItems.Count > 0)
                {
                    var keys = BulbKeysBuilder.BuildMenuKeys(bulbItems);
                    writer.WriteLine("QUICKFIXES:");

                    using (ReadLockCookie.Create())
                    {
                        EventHandler<BeforeAcquiringWriteLockEventArgs> assertWriteLock = (sender, args) =>
                        {
                            Assert.Fail(
                                "Cannot take WriteLock in QuickFix.IsAvailabe(). WriteLock can be taken only on Primary thread.");
                        };

                        locks.ContentModelLocks.BeforeAcquiringWriteLock += assertWriteLock;
                        try
                        {
                            BulbMenuTestBase.Write(keys, "", writer);
                        }
                        finally
                        {
                            locks.ContentModelLocks.BeforeAcquiringWriteLock -= assertWriteLock;
                        }
                    }
                }
                else
                {
                    writer.WriteLine("NO QUICKFIXES");
                }
            }
        }

        private sealed class HighlightingComparer : IComparer<HighlightingInfo>
        {
            public int Compare(HighlightingInfo x, HighlightingInfo y)
            {
                var compare = x.Range.TextRange.StartOffset.CompareTo(y.Range.TextRange.StartOffset);
                return (compare == 0) ? String.CompareOrdinal(x.Highlighting.ToolTip, y.Highlighting.ToolTip) : compare;
            }

            public static readonly HighlightingComparer Instance = new HighlightingComparer();
        }

        private sealed class DaemonProcessMock : TestDaemonProcess
        {
            public DaemonProcessMock([NotNull] IPsiSourceFile sourceFile)
                : base(sourceFile)
            {
                Highlightings = new List<HighlightingInfo>();
            }

            public List<HighlightingInfo> Highlightings { get; private set; }

            protected override void CommitHighlighters(DaemonCommitContext context)
            {
                Highlightings.AddRange(context.HighlightingsToAdd);
            }
        }
    }
}