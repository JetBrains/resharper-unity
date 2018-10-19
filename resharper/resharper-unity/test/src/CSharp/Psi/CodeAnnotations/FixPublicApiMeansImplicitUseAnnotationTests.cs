using JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.UsageChecking;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.CodeAnnotations
{
    [TestUnity]
    public class FixPublicApiMeansImplicitUseAnnotationTests : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeAnnotations";

        [Test] public void TestPublicApiAnnotation() { DoNamedTest2(); }
    }
}