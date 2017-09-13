using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon.Stages.Analysis
{
    public class ShaderLabPreprocessorHighlightingTests : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is ShaderLabErrorPreprocessorDirectiveError
                || highlighting is ShaderLabWarningPreprocessorDirectiveWarning
                || highlighting is ShaderLabSwallowedPreprocessorCharWarning
                || highlighting is ShaderLabSyntaxError;
        }

        [Test] public void TestShaderLabPreprocessorDirectiveHighlights() { DoNamedTest2(); }
    }
}