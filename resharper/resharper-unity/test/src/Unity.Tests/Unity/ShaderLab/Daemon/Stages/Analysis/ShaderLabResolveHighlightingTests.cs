using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [RequireHlslSupport]
    public class ShaderLabResolveHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("ShaderLab", "Highlighting", "Unresolved and ambiguous properties")] public void TestUnresolvedPropertyHighlights() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Highlighting", "Unresolved and ambiguous properties")] public void TestMultipleCandidatePropertyHighlights() { DoNamedTest2(); }
    }
}