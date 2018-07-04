using JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.UsageChecking;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.CodeAnnotations
{
    [TestUnity]
    public class FixPublicApiMeansImplicitUseAnnotationTests : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"psi\CodeAnnotations";

        [Test] public void TestPublicApiAnnotation() { DoNamedTest2(); }
    }
}