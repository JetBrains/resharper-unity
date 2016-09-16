using ApiParser;
using NUnit.Framework;

namespace ApiParserTest
{
    [TestFixture]
    public class TestXPath
    {
        [Test]
        public void TestClassTranslation()
        {
            var xpath = "/div.some.class/div.some.other.class/h1";
            const string expected = @"/div[@class='some class']/div[@class='some other class']/h1";

            xpath = XPath.Resolve(xpath);

            Assert.AreEqual(expected, xpath);
        }

        [Test]
        public void TestCombined()
        {
            var xpath = "/div.some.class[h1='test']/text()";
            const string expected = @"/div[@class='some class' and h1='test']/text()";

            xpath = XPath.Resolve(xpath);

            Assert.AreEqual(expected, xpath);
        }

        [Test]
        public void TestIdTranslation()
        {
            var xpath = "/div.some.class/div#unique/h1";
            const string expected = @"/div[@class='some class']/div[@id='unique']/h1";

            xpath = XPath.Resolve(xpath);

            Assert.AreEqual(expected, xpath);
        }

        [Test]
        public void TestClassAndIdTranslation()
        {
            var xpath = "/div.some.class/div#unique.other.class/h1";
            const string expected = @"/div[@class='some class']/div[@id='unique' and @class='other class']/h1";

            xpath = XPath.Resolve(xpath);

            Assert.AreEqual(expected, xpath);
        }
    }
}
