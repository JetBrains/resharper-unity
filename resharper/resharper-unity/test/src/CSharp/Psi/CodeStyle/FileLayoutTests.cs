using JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.CodeStyle
{
    // This test does not currently run on Mac, because the system Mono's System.Xaml.dll expects a single mapping
    // between CLR and XAML namespaces in [assembly: XmlnsDefinition(...)], and ReSharper has two attributes to map a
    // XAML namespaces to the JetBrains.Application.Src.UI.Icons CLR namespace. The System.Xaml shipping with ReSharper
    // uses a dictionary indexer instead of the Add method, so it works, although last write wins
    [TestUnity]
    public class FileLayoutTests : CodeCleanupTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeStyle";

        [Test] public void TestFileLayout01() { DoNamedTest2(); }
    }
}