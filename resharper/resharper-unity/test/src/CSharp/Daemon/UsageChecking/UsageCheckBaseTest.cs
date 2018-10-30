using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.UsageChecking
{
    public abstract class UsageCheckBaseTest : BaseTestWithSingleProject
    {
        protected override void DoTest(IProject project)
        {
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (TestPresentationMap.Cookie())
            using (TestPsiConfigurationSettings.Instance.PersistCachesCookie())
            using (swea.RunAnalysisCookie())
            {
                Lifetimes.Using(lifetime =>
                {
                    ChangeSettingsTemporarily(lifetime).BoundStore.SetValue(HighlightingSettingsAccessor.AnalysisEnabled, AnalysisScope.SOLUTION);

                    var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                    foreach (var file in files)
                        swea.AnalyzeInvisibleFile(file);

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
                });
            }
        }
    }
}