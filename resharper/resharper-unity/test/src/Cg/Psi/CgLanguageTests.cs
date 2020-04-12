using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Cg.Psi
{
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