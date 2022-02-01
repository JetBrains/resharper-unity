using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [RequireHlslSupport]
    public class ShaderLabResolveHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test] public void TestUnresolvedPropertyHighlights() { DoNamedTest2(); }
        [Test] public void TestMultipleCandidatePropertyHighlights() { DoNamedTest2(); }
    }
}