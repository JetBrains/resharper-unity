using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon.Stages.Analysis
{
    public class ShaderLabResolveHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test] public void TestUnresolvedPropertyHighlights() { DoNamedTest2(); }
        [Test] public void TestMultipleCandidatePropertyHighlights() { DoNamedTest2(); }
    }
}