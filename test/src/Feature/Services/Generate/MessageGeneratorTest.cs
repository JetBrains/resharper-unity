using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.Generate
{
    [TestFileExtension(CSharpProjectFileType.CS_EXTENSION)]
    [TestUnity]
    public class MessageGeneratorTest : GenerateTestBase
    {
        protected override string RelativeTestDataPath => @"generate";

        [Test] public void MonoBehaviour01() { DoNamedTest(); }

        [Test] public void ListElements01() { DoNamedTest(); }
        // TODO: Deriving from AssetModificationProcessor doesn't work. Don't know why
        // [Test] public void ListElements02() { DoNamedTest(); }
        [Test] public void ListElements03() { DoNamedTest(); }
        [Test] public void ListElements04() { DoNamedTest(); }
        [Test] public void ListElements05() { DoNamedTest(); }
        [Test] public void ListElements06() { DoNamedTest(); }
        [Test] public void ListElements07() { DoNamedTest(); }
        [Test] public void ListElements08() { DoNamedTest(); }
        [Test] public void ListElements09() { DoNamedTest(); }
        [Test] public void ListElements10() { DoNamedTest(); }
        [Test] public void ListElements11() { DoNamedTest(); }
        [Test] public void ListElements12() { DoNamedTest(); }
        [Test] public void ListElements13() { DoNamedTest(); }
        [Test] public void ListElements14() { DoNamedTest(); }
        [Test] public void ListElements15() { DoNamedTest(); }
        [Test] public void ListElements16() { DoNamedTest(); }
        [Test] public void ListElements17() { DoNamedTest(); }
        [Test] public void ListElements18() { DoNamedTest(); }
        [Test] public void ListElements19() { DoNamedTest(); }
    }
}