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
using JetBrains.TestFramework.Projects;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    [TestFixture]
    [ReuseSolution(false)]
    public class RedundantSerializedFieldAttributeTest : BaseTestWithExistingSolution
    {
        protected override string RelativeTestDataPath => "RedundantSerializedFieldAttributeTest";
        private const string AssembliesDirectory = "Assemblies";

        [Test]
        public void TestSerialisedReferenceFromSourceCode()
        {
            var testSolutionAbsolutePath = GetTestDataFilePath2(@"Solutions\Test001\Test001.sln");
            DoSolutionTestWithGold(testSolutionAbsolutePath);
        }

        [Test]
        public void TestWithSerialisedReferenceFromAssemblies()
        {
            var testSolutionAbsolutePath = GetTestDataFilePath2(@"Solutions\Test002\Test002.sln");
            SolutionBuilderHelper.PrepareDependencies(BaseTestDataPath, testSolutionAbsolutePath, "AssemblyWithSerializedRef", AssembliesDirectory);
            DoSolutionTestWithGold(testSolutionAbsolutePath);
        }


        [Test]
        public void TestWithoutSerialisedReferenceFromAssemblies()
        {
            var testSolutionAbsolutePath = GetTestDataFilePath2(@"Solutions\Test003\Test003.sln");
            SolutionBuilderHelper.PrepareDependencies(BaseTestDataPath, testSolutionAbsolutePath, "AssemblyWithoutSerilaizeRef", AssembliesDirectory);
            DoSolutionTestWithGold(testSolutionAbsolutePath);
        }

        [Test]
        public void RedundantFieldsWithGenerics()
        {
            var testSolutionAbsolutePath = GetTestDataFilePath2(@"Solutions\RedundantFieldsWithGenerics\RedundantFieldsWithGenerics.sln");
            DoSolutionTestWithGold(testSolutionAbsolutePath);
        }

        private void DoSolutionTestWithGold(FileSystemPath solutionPath)
        {
            DoTestSolution(solutionPath, TestAction);
        }

        private void TestAction(Lifetime lt, ISolution solution)
        {
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (TestPresentationMap.Cookie())
            using (UnityProjectCookie.RunUnitySolutionCookie(solution))
            using (swea.RunAnalysisCookie())
            {
                swea.ReanalyzeAll();
                var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                foreach (var file in files) swea.AnalyzeInvisibleFile(file);

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

        protected virtual bool IsTestFile(IPsiSourceFile file)
        {
            if (file.LanguageType.IsNullOrUnknown()) return false;

            var pf = file.ToProjectFile();
            if (pf == null) return false;

            return pf.Location.Name.EndsWith(".cs") && file.Name.Contains("TestFile");
        }

        protected virtual bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is IUnityAnalyzerHighlighting;
        }
    }
}