using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class RedundantEventFunctionWarningTests : CSharpHighlightingTestBase
    {
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is RedundantEventFunctionWarning || highlighting is RedundantOverriddenMemberWarning;
        }

        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestRedundantEventFunctionWarning() { DoNamedTest2(); }
    }
}