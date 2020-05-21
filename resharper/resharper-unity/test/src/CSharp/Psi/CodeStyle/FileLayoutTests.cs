using JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.CodeStyle
{
    [TestUnity]
    public class FileLayoutTests : CodeCleanupTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeStyle";

        [Test] public void TestFileLayout01() { DoNamedTest2(); }
    }
}