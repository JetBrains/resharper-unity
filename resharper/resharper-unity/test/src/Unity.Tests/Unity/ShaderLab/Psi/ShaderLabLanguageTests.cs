using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Psi
{
    [RequireHlslSupport]
    [TestFixture]
    public class ShaderLabLanguageTests : BaseTest
    {
        [Test]
        public void LanguageIsRegistered()
        {
            Assert.NotNull(ShaderLabLanguage.Instance);
            Assert.NotNull(Languages.Instance.GetLanguageByName(ShaderLabLanguage.Name));
        }
    }
}