using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [RequireHlslSupport]
    public class ShaderLabInvalidParametersOnVariableReferenceHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test] public void TestInvalidParametersOnVariableReferenceHighlights() { DoNamedTest2(); }
    }
}