using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;

[TestUnity]
public class UnityObjectLifetimeCheckViaNullEqualityWarningTests : CSharpHighlightingTestBase<UnityObjectLifetimeCheckViaNullEqualityWarning>
{
    protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

    [Test] public void TestUnityObjectLifetimeCheckViaNullEqualityWarning() { DoNamedTest2(); }    
}