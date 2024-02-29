using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;

[TestUnity]
[TestCustomInspectionSeverity(UnityObjectNullPatternMatchingWarning.HIGHLIGHTING_ID, Severity.WARNING)]
public class UnityObjectNullPatternMatchingWarningTests : CSharpHighlightingTestBase<UnityObjectNullPatternMatchingWarning>
{
    protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

    [Test] public void TestUnityObjectNullPatternMatchingWarning() { DoNamedTest2(); }
}