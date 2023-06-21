using JetBrains.ReSharper.Feature.Services.Cpp.Daemon.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Hlsl
{
    [RequireHlslSupport]
    public class HlslCodeAnalysisTest : ShaderLabHighlightingTestBase<CppHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Hlsl\Analysis";

        // it is ok that fixed4 and other macros are not resolved, because we do not have UnityCg folder
        [Test] public void TestHlsl01() { DoNamedTest2(); }
        [Test] public void TestHlsl02() { DoNamedTest2(); }
        [Test] public void TestHlsl03() { DoNamedTest2(); }
        [Test] public void TestIncludes() { DoTestSolution("Includes/Test01.shader", "Includes/Used.hlsl", "Includes/Unused.hlsl"); }
    }
}