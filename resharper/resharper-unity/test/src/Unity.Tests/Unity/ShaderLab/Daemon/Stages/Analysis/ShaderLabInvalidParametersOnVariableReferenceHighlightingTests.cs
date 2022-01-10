using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Analysis
{
    public class ShaderLabInvalidParametersOnVariableReferenceHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test] public void TestInvalidParametersOnVariableReferenceHighlights() { DoNamedTest2(); }
    }
}