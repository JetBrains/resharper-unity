using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    public abstract class UnitySerializationGlobalStageTestBase<TUnityHighlighting> : BaseTestWithSingleProject
        where TUnityHighlighting : IUnityAnalyzerHighlighting
    {
        protected override string GetGoldTestDataPath(string fileName)
        {
            return base.GetGoldTestDataPath(fileName + ".global");
        }

        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (TestPresentationMap.Cookie())
            using (UnityProjectCookie.RunUnitySolutionCookie(Solution))
            using (swea.RunAnalysisCookie())
            {
                swea.ReanalyzeAll();
                var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();

                foreach (var file in files)
                    swea.AnalyzeInvisibleFile(file);

                swea.AllFilesAnalyzed();

                ExecuteWithGold(GetTestDataFilePath2(TestName), writer =>
                {
                    foreach (var file in files)
                        if (IsTestFile(file))
                        {
                            var process = new TestHighlightingDumperWithOverridenStages(file, writer,
                                DaemonStageManager.GetInstance(Solution).Stages, HighlightingPredicate,
                                CSharpLanguage.Instance);
                            process.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
                            process.DoHighlighting(DaemonProcessKind.GLOBAL_WARNINGS);
                            process.Dump();
                        }
                });
            }
        }

        private bool IsTestFile(IPsiSourceFile file)
        {
            return file.Name == TestName2;
        }

        protected virtual bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is TUnityHighlighting;
        }
    }
}