using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.SolutionAnalysis;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions
{
    public abstract class CSharpQuickFixAfterSwaTestBase<TQuickFix> : CSharpQuickFixTestBase<TQuickFix>
        where TQuickFix : IQuickFix
    {
        protected override void DoTestSolution(params string[] fileSet)
        {
            using (TestPresentationMap.Cookie())
            {
                base.DoTestSolution(fileSet);
            }
        }

        protected override QuickFixInstance? CreateBulbAction(IProject project, ITextControl textControl)
        {
            var solution = project.GetSolution();
            var solutionAnalysisService = solution.GetComponent<SolutionAnalysisService>();

            using (solutionAnalysisService.RunAnalysisCookie())
            using (UnityProjectCookie.RunUnitySolutionCookie(solution))
            {
                foreach (var file in solutionAnalysisService.GetFilesToAnalyze())
                    solutionAnalysisService.AnalyzeInvisibleFile(file);

                solutionAnalysisService.AllFilesAnalyzed();

                using (SyncReanalyzeCookie.Create(solution.Locks, SolutionAnalysisManager.GetInstance(solution)))
                {
                    var errorInfo = RunErrorFinder(project, textControl, typeof(TQuickFix), DaemonProcessKind.GLOBAL_WARNINGS);

                    var quickFixes = Shell.Instance.GetComponent<IQuickFixes>();

                    var quickFix = quickFixes.InstantiateQuickFixNoAvailabilityCheck(errorInfo.Highlighting, typeof(TQuickFix), quickFixIndex: 0);
                    if (quickFix is null) return null;

                    return new QuickFixInstance(quickFix, errorInfo);
                }
            }
        }

        protected override void OnQuickFixNotAvailable(ITextControl textControl, IQuickFix action)
        {
            textControl.PutData(TextControlBannerKey, "NOT_AVAILABLE\r\n");
        }
    }
}