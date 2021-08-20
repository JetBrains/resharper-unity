using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml.Psi
{
  [TestFixture]
  public class YamlLanguageTests : BaseTest
  {
    [Test]
    public void LanguageIsRegistered()
    {
      Assert.NotNull(YamlLanguage.Instance);
      Assert.NotNull(Languages.Instance.GetLanguageByName(YamlLanguage.Name));
    }
  }
}