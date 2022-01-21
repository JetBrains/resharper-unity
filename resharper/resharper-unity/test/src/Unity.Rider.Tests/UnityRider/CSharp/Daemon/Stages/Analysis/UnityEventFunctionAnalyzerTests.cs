using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.UnityRider.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityEventFunctionAnalyzerTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                                      IContextBoundSettingsStore settingsStore)
        {
            return highlighting is IUnityIndicatorHighlighting;
        }

        // ********************************************************************
        // IMPORTANT! Keep in sync with equivalent class in Unity.Tests
        // ********************************************************************

        [Test] public void TestUnityEventFunctionAnalyzer() { DoNamedTest2(); }

        protected override IList<string> TraceCategories()
        {
            myLogger.Info($"MyConfigPath1: {Environment.GetEnvironmentVariable("RESHARPER_LOG_CONF")}");
            myLogger.Info($"MyConfigPath2: {Environment.GetEnvironmentVariable("RESHARPER_HOST_LOG_DIR")}");
            var result = base.TraceCategories().ToList();
            result.Add("JetBrains.Application.Environment"); 
            return result;
        }
    }
}