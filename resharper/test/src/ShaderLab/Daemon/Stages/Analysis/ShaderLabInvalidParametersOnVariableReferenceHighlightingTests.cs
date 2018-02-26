using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon.Stages.Analysis
{
    public class ShaderLabInvalidParametersOnVariableReferenceHighlightingTests : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is ShaderLabHighlightingBase;
        }

        [Test] public void TestInvalidParametersOnVariableReferenceHighlights() { DoNamedTest2(); }
    }
}