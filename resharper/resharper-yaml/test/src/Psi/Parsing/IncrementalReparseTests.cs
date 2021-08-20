using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml.Psi.Parsing
{
  [TestFileExtension(TestYamlProjectFileType.YAML_EXTENSION)]
  public class IncrementalReparseTests : IncrementalReparseTestBase
  {
    protected override string RelativeTestDataPath => @"Psi\Parsing\Reparse";

    [Test] public void Test01() { DoNamedTest(); }
    [Test] public void Test02() { DoNamedTest(); }
    [Test] public void Test03() { DoNamedTest(); }
    [Test] public void Test04() { DoNamedTest(); }
    [Test] public void Test05() { DoNamedTest(); }
    [Test] public void Test06() { DoNamedTest(); }
    [Test] public void Test07() { DoNamedTest(); }
    [Test] public void Test08() { DoNamedTest(); }
    [Test] public void Test09() { DoNamedTest(); }
    [Test] public void Test10() { DoNamedTest(); }
    [Test] [Ignore("TODO: vkrasnotsvetov, introduce reparse for new chameleons")] public void Test11() { DoNamedTest(); }
    [Test] [Ignore("TODO: vkrasnotsvetov, introduce reparse for new chameleons")] public void Test12() { DoNamedTest(); }

    [Test] public void TestDelete01() { DoNamedTest2(); }
    [Test] public void TestDelete02() { DoNamedTest2(); }
  }
}