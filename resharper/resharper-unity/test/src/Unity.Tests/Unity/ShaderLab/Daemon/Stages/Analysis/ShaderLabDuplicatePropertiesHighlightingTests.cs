using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Analysis
{
    [RequireHlslSupport]
    public class ShaderLabDuplicatePropertiesHighlightingTests : ShaderLabHighlightingTestBase<ShaderLabHighlightingBase>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("ShaderLab", "Highlighting", "Duplicate properties")] public void TestDuplicatePropertyHighlights() { DoNamedTest2(); }
    }
}