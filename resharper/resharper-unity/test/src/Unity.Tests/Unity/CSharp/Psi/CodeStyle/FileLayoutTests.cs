using JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Psi.CodeStyle
{
    // This test does not currently run on Mac, because the system Mono's System.Xaml.dll expects a single mapping
    // between CLR and XAML namespaces in [assembly: XmlnsDefinition(...)], and ReSharper has two attributes to map a
    // XAML namespaces to the JetBrains.Application.Src.UI.Icons CLR namespace. The System.Xaml shipping with ReSharper
    // uses a dictionary indexer instead of the Add method, so it works, although last write wins
    [TestUnity]
    public class FileLayoutTests : CodeCleanupTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeStyle";

        [Test]
        public void TestFileLayout01()
        {
            // This test does not currently run on Mono, because the System.Xaml.dll that ships with system Mono (that
            // runs tests) expects a single mapping between CLR and XAML namespaces in [assembly: XmlnsDefinition(...)],
            // and ReSharper has two attributes to map a XAML namespaces to the JetBrains.Application.Src.UI.Icons CLR
            // namespace. The System.Xaml shipping with Rider uses a dictionary indexer instead of the Add method,
            // so it works, although last write wins
            // It does work when run as part of the monorepo, presumably because it picks up a working version of
            // System.Xaml.dll from the product bin folder
#if !INDEPENDENT_BUILD
            if (Util.PlatformUtil.IsRunningOnMono)
                Assert.Ignore("Test not supported on Mono");
#endif
            DoNamedTest2();
        }
    }
}