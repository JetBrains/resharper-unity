using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Yaml.Tests.Psi
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