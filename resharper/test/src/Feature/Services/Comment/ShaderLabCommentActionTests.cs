using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.Comment
{
    [TestFileExtension(".shader")]
    [TestFixture]
    public class ShaderLabCommentActionTests : ExecuteActionTestBase
    {
        protected override string RelativeTestDataPath => @"comment";

        [Test] public void TestLineComment() { DoNamedTest2(); }
        [Test] public void TestLineUncomment() { DoNamedTest2(); }
        [Test] public void TestMultiLineComment() { DoNamedTest2(); }
        [Test] public void TestMultiLineUncomment() { DoNamedTest2(); }
        [Test] public void TestBlockComment() { DoNamedTest2(); }
        [Test] public void TestBlockUncomment() { DoNamedTest2(); }
    }
}