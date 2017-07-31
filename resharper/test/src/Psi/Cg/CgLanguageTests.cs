using JetBrains.ReSharper.Plugins.Unity.Psi.Cg;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.Cg
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