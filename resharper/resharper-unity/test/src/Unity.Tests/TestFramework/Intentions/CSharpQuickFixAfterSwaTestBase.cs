using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.SolutionAnalysis;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions
{
    public abstract class CSharpQuickFixAfterSwaTestBase<TQuickFix> : CSharpQuickFixTestBase<TQuickFix> where TQuickFix : IQuickFix
    {
        protected override void DoTestSolution(params string[] fileSet)
        {
            using (TestPresentationMap.Cookie())
            {
                base.DoTestSolution(fileSet);
            }
        }

        protected override IQuickFix CreateQuickFix(IProject project, ITextControl textControl,
            out IHighlighting highlighting)
        {
            IQuickFix result;
            var solution = project.GetSolution();
            var swea = solution.GetComponent<SolutionAnalysisService>();
            using (swea.RunAnalysisCookie())
            using (UnityProjectCookie.RunUnitySolutionCookie(solution))
            {
                foreach (var file in swea.GetFilesToAnalyze())
                    swea.AnalyzeInvisibleFile(file);
                
                swea.AllFilesAnalyzed();

                using (SyncReanalyzeCookie.Create(solution.Locks, SolutionAnalysisManager.GetInstance(solution)))
                {
                    highlighting = RunErrorFinder(project, textControl, typeof(TQuickFix), DaemonProcessKind.GLOBAL_WARNINGS);
                    result = Shell.Instance.GetComponent<IQuickFixes>().InstantiateQuickfix(highlighting, typeof(TQuickFix), 0);
                }
            }

            return result;
        }

        protected override void OnQuickFixNotAvailable(ITextControl textControl, IQuickFix action)
        {
            textControl.PutData(TextControlBannerKey, "NOT_AVAILABLE\r\n");
        }
    }
}