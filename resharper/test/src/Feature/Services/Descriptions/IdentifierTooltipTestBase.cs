﻿using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.Descriptions
{
    // The default implementation doesn't handle resolving overlapping highlightings
    [Category("Daemon")]
    public abstract class IdentifierTooltipTestBase : BaseTestWithTextControl
    {
        protected override void DoTestSolution(params string[] fileSet)
        {
            ExecuteWithinSettingsTransaction(store =>
            {
                RunGuarded(() => store.SetValue(HighlightingSettingsAccessor.IdentifierHighlightingEnabled, true));
                base.DoTestSolution(fileSet);
            });
        }

        protected override void DoTest(IProject testProject)
        {
            testProject.GetSolution().GetPsiServices().Files.CommitAllDocuments();
            using (ITextControl textControl = OpenTextControl(testProject))
            {
                var document = textControl.Document;
                var psiSourceFile = document.GetPsiSourceFile(Solution);
                Assert.IsNotNull(psiSourceFile, "sourceFile == null");
                using (ReadLockCookie.Create())
                {
                    var highlightingFinder = new IdentifierHighlightingFinder(psiSourceFile, new DocumentRange(document, new TextRange(textControl.Caret.Offset())));
                    highlightingFinder.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
                    var highlightingInfo = highlightingFinder.HighlightingInfo;
                    Assertion.AssertNotNull(highlightingInfo, "Highlighting not found");
                    var markupModel = Solution.GetComponent<IDocumentMarkupManager>().GetMarkupModel(document);
                    var highlighterTooltipProvider = DaemonUtil.GetHighlighterTooltipProvider(highlightingInfo.Highlighting, Solution);
                    #if RIDER
                        var attributeId = HighlightingSettingsManager.Instance.GetAttributeId(highlightingInfo.Highlighting, psiSourceFile, Solution).NotNull();
                    #else
                        var attributeId = HighlightingSettingsManager.Instance.GetAttributeId(highlightingInfo.Highlighting, psiSourceFile).NotNull();
                    #endif
                    var highlighter = markupModel.AddHighlighter("test", highlightingInfo.Range.TextRange, AreaType.EXACT_RANGE, 0, attributeId, new ErrorStripeAttributes(), highlighterTooltipProvider, null);
                    ExecuteWithGold(writer => writer.WriteLine(highlighter.ToolTip));
                }
            }
        }

        private class IdentifierHighlightingFinder : TestDaemonProcess
        {
            private readonly DocumentRange myCaretRange;

            public HighlightingInfo HighlightingInfo { get; private set; }

            public IdentifierHighlightingFinder([NotNull] IPsiSourceFile sourceFile, DocumentRange caretRange)
                : base(sourceFile)
            {
                myCaretRange = caretRange;
            }

            public override void CommitHighlighters(DaemonCommitContext context)
            {
                HighlightingSettingsManager instance = HighlightingSettingsManager.Instance;
                foreach (HighlightingInfo highlightingInfo in context.HighlightingsToAdd)
                {
                    var severity =
                        #if RIDER
                            instance.GetSeverity(highlightingInfo.Highlighting, SourceFile, Solution);
                        #else
                            instance.GetSeverity(highlightingInfo.Highlighting, SourceFile);
                        #endif
                    if (highlightingInfo.Range.Contains(myCaretRange) && severity == Severity.INFO)
                    {
                        if (highlightingInfo.Highlighting is CSharpIdentifierHighlighting)
                            HighlightingInfo = highlightingInfo;
                        break;
                    }
                }
            }
        }
    }
}
