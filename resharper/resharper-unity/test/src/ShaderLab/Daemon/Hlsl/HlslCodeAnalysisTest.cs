using JetBrains.ReSharper.Psi.Cpp.Daemon;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon.Hlsl
{
    public class HlslCodeAnalysisTest : ShaderLabHighlightingTestBase<CppHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Hlsl\Analysis";

        // it is ok that fixed4 and other macroses are not resolved, because we do not have UnityCg folder
        [Test] public void TestHlsl01() { DoNamedTest2(); }
        [Test] public void TestHlsl02() { DoNamedTest2(); }
        [Test] public void TestHlsl03() { DoNamedTest2(); }
    }
}