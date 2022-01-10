using System;
using System.Linq;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.UnityEditorIntegration
{
    [TestFixture]
    public class UnityVersionTest
    {
        [Test]
        public void VersionConversionTest()
        {
            var marketingVersion = "2018.2.13p1";
            var realVersion = Version.Parse("2018.2.13.128001");
            Assert.AreEqual(realVersion, Plugins.Unity.UnityEditorIntegration.UnityVersion.Parse(marketingVersion));
            Assert.AreEqual(marketingVersion, Plugins.Unity.UnityEditorIntegration.UnityVersion.VersionToString(realVersion));
        }

        [Test]
        public void CustomUnityVersionConversionTest()
        {
            var marketingVersion = "2017.2.1f1-CustomPostfix";
            var realVersion = Version.Parse("2017.2.1.118001");
            Assert.AreEqual(realVersion, Plugins.Unity.UnityEditorIntegration.UnityVersion.Parse(marketingVersion));
            Assert.AreEqual("2017.2.1f1", Plugins.Unity.UnityEditorIntegration.UnityVersion.VersionToString(realVersion));
        }

        [Test]
        public void VersionToStringTest()
        {
            var marketingVersion = "2018.2.13";
            var realVersion = Version.Parse("2018.2.13.0");

            Assert.AreEqual(marketingVersion, Plugins.Unity.UnityEditorIntegration.UnityVersion.VersionToString(realVersion));
        }

        [Test]
        public void SortTest()
        {
            var mVersions = new[] {"2018.2.13a20",  "2018.2.13b1", "2018.2.13b3", "2018.2.13b10", "2018.2.13b20", "2018.2.13f1","2018.2.13f20", "2018.2.13p1", "2018.2.13p20"};
            var expected = new[] {"2018.2.13", "2018.2.13a20",  "2018.2.13b1", "2018.2.13b3", "2018.2.13b10", "2018.2.13b20", "2018.2.13f1","2018.2.13f20", "2018.2.13p1", "2018.2.13p20"};
            var actualVersions = mVersions.Select(Plugins.Unity.UnityEditorIntegration.UnityVersion.Parse).ToList();
            actualVersions.Add(Version.Parse("2018.2.13"));
            actualVersions = actualVersions.OrderBy().ToList();
            var actual = actualVersions.Select(Plugins.Unity.UnityEditorIntegration.UnityVersion.VersionToString).ToArray(); // Select preserves Ordering
            Assert.AreEqual(expected, actual);
        }
    }
}