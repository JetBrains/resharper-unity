using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Psi.CodeAnnotations
{
    [TestUnity]
    public class FixPublicApiMeansImplicitUseAnnotationTests : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeAnnotations";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Code annotations", "PublicAPI implicit use annotation")] public void TestPublicApiAnnotation() { DoNamedTest2(); }
    }
}