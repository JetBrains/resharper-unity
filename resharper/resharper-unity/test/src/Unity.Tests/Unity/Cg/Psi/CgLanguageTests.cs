using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Cg.Psi
{
    [RequireHlslSupport]
    [TestFixture]
    public class CgLanguageTests
    {
        [Test]
        public void LanguageIsRegistered()
        {
            Assert.NotNull(CgLanguage.Instance);
            Assert.NotNull(Languages.Instance.GetLanguageByName(CgLanguage.Name));
        }
    }
}