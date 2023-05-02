using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.TestFramework
{
    public abstract class UsageCheckBaseTest : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\UsageChecking";

        protected virtual bool DisableYamlParsing() => false;

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (TestPresentationMap.Cookie())
            using (TestPsiConfigurationSettings.Instance.PersistCachesCookie())
            using (swea.RunAnalysisCookie())
            {
                ChangeSettingsTemporarily(lifetime).BoundStore.SetValue(HighlightingSettingsAccessor.AnalysisEnabled, AnalysisScope.SOLUTION);
                if (DisableYamlParsing())
                    ChangeSettingsTemporarily(lifetime).BoundStore.SetValue((UnitySettings key) => key.IsAssetIndexingEnabled, false);

                var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                foreach (var file in files)
                    swea.AnalyzeInvisibleFile(file);

                swea.AllFilesAnalyzed();

                ExecuteWithGold(writer =>
                {
                    var highlightingSettingsManager = HighlightingSettingsManager.Instance;
                    foreach (var file in files)
                    {
                        if (file.LanguageType.IsNullOrUnknown()) continue;
                        var pf = file.ToProjectFile();
                        if (pf == null) continue;
                        if (!pf.Location.Name.EndsWith(".cs")) continue;

                        var process = new TestHighlightingDumper(file, writer,
                            DaemonStageManager.GetInstance(Solution).Stages,
                            (highlighting, psiSourceFile, settingsStore) =>
                            {
                                if (highlighting is IHighlightingTestBehaviour { IsSuppressed: true }) return false;

                                var attribute = highlightingSettingsManager.GetHighlightingAttribute(highlighting);
                                var severity = highlightingSettingsManager.GetSeverity(highlighting, psiSourceFile, Solution, settingsStore);
                                return severity != Severity.INFO || attribute.OverlapResolve != OverlapResolveKind.NONE;
                            },
                            CSharpLanguage.Instance);
                        process.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
                        process.DoHighlighting(DaemonProcessKind.GLOBAL_WARNINGS);
                        process.Dump();
                        writer.WriteLine();
                    }
                });
            }
        }
    }
}