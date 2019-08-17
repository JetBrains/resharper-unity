using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.CodeAnnotations
{
    [TestUnity]
    public class FixCharacterControllerCollisionFlagAnnotationTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeAnnotations";

        [Test] public void TestCharacterControllerCollisionFlagsAnnotation() { DoNamedTest2(); }
    }
}