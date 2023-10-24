using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Uxml.Psi
{
    [TestFixture]
    public class UxmlLanguageTests
    {
        [Test]
        public void LanguageIsRegistered()
        {
            Assert.NotNull(UxmlLanguage.Instance);
            Assert.NotNull(Languages.Instance.GetLanguageByName(UxmlLanguage.Name));
        }
    }
}