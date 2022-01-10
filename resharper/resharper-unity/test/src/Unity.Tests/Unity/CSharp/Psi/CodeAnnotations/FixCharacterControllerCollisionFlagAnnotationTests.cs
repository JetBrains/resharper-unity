using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Psi.CodeAnnotations
{
    // See RIDER-28661
    [TestUnity]
    public class FixCharacterControllerCollisionFlagAnnotationTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeAnnotations";

        [Test] public void TestCharacterControllerCollisionFlagsAnnotation() { DoNamedTest2(); }
    }
}