using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.CSharp.Daemon.Stages.GutterMarks
{
    [TestUnity]
    public class GutterMarkTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\GutterMark";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                                      IContextBoundSettingsStore settingsStore)
        {
            return highlighting is IUnityIndicatorHighlighting;
        }

        // ********************************************************************
        // IMPORTANT! Keep in sync with equivalent class in Unity.Tests
        // ********************************************************************

        [Test] public void Test01() { DoNamedTest(); }

        [Test, TestUnity(UnityVersion.Unity2019_4)] public void TestGenericSerialisedFields_2019_4() { DoNamedTest2(); }
        [Test, TestUnity(UnityVersion.Unity2020_1)] public void TestGenericSerialisedFields_2020_1() { DoNamedTest2(); }
    }
}