using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [Category("Daemon"), Category("PerformanceCriticalCode")]
    public abstract class PerformanceCriticalCodeStageTestBase : BaseTestWithSingleProject
    {
        protected override void DoTest(IProject project)
        {
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (TestPresentationMap.Cookie())
            using (TestPsiConfigurationSettings.Instance.PersistCachesCookie())
            using (swea.RunAnalysisCookie())
            {
                Lifetime.Using(lifetime =>
                {
                    ChangeSettingsTemporarily(lifetime).BoundStore.SetValue(HighlightingSettingsAccessor.AnalysisEnabled, AnalysisScope.SOLUTION);

                    var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                    foreach (var file in files)
                        swea.AnalyzeInvisibleFile(file);

                    ExecuteWithGold(this.TestMethodName + ".cs", writer =>
                    {
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
                                    return highlighting is PerformanceHighlightingBase;
                                },
                                CSharpLanguage.Instance);
                            process.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
                            process.DoHighlighting(DaemonProcessKind.GLOBAL_WARNINGS);
                            process.Dump();
                        }
                    });
                });
            }
        }
    }
}
