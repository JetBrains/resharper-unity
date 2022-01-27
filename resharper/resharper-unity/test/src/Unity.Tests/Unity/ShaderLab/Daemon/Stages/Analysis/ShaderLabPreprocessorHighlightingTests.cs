using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [RequireHlslSupport]
    public class ShaderLabPreprocessorHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test] public void TestShaderLabPreprocessorDirectiveHighlights() { DoNamedTest2(); }
    }
}