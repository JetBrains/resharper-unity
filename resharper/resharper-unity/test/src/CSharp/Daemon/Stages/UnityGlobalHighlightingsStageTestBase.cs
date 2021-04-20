using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages
{
    [Category("Daemon"), Category("PerformanceCriticalCode")]
    public abstract class UnityGlobalHighlightingsStageTestBase : BaseTestWithSingleProject
    {
        protected sealed override string RelativeTestDataPath=> $@"{RelativeTestDataRoot}\{Utils.ProductGoldSuffix}";
        protected abstract string RelativeTestDataRoot { get; }
        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            var swea = SolutionAnalysisService.GetInstance(Solution);
            using (TestPresentationMap.Cookie())
            using (TestPsiConfigurationSettings.Instance.PersistCachesCookie())
            using (swea.RunAnalysisCookie())
            {
                ChangeSettingsTemporarily(lifetime).BoundStore.SetValue((UnitySettings key) => 
                    key.PerformanceHighlightingMode, PerformanceHighlightingMode.Always);

                var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                swea.ReanalyzeAll();
                
                ExecuteWithGold(TestMethodName + ".cs", writer =>
                {
                    foreach (var file in files)
                    {
                        if (file.LanguageType.IsNullOrUnknown()) continue;
                        var pf = file.ToProjectFile();
                        if (pf == null) continue;
                        if (!pf.Location.Name.EndsWith(".cs")) continue;

                        var process = new TestHighlightingDumperWithOverridenStages(file, writer,
                            DaemonStageManager.GetInstance(Solution).Stages,
                            HighlightingPredicate,
                            CSharpLanguage.Instance);
                        process.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
                        process.DoHighlighting(DaemonProcessKind.GLOBAL_WARNINGS);
                        process.Dump();
                    }
                });
            }
        }

        protected abstract bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile file,
            IContextBoundSettingsStore settingsStore);
    }
}
