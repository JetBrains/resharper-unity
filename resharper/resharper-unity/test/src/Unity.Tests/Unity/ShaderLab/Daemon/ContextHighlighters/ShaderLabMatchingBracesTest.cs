using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.ContextHighlighters
{
    [RequireHlslSupport]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabMatchingBracesTest : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"Braces";

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestBraces01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestBraces02() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestBracks01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestBracks02() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestParens01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestParens02() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestQuotes01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestQuotes02() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestCg01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Matching braces")] public void TestCg02() { DoNamedTest2(); }
    }
}