using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.Comment
{
    [RequireHlslSupport]
    [TestFileExtension(".shader")]
    [TestFixture]
    public class ShaderLabCommentActionTests : ExecuteActionTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Comment";

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Comment and uncomment")] public void TestLineComment() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Comment and uncomment")] public void TestLineUncomment() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Comment and uncomment")] public void TestMultiLineComment() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Comment and uncomment")] public void TestMultiLineUncomment() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Comment and uncomment")] public void TestBlockComment() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Comment and uncomment")] public void TestBlockUncomment() { DoNamedTest2(); }
    }
}