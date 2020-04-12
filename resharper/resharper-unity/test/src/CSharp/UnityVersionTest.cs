using System;
using System.Linq;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [TestFixture]
    public class UnityVersionTest
    {
        [Test]
        public void VersionConversionTest()
        {
            var marketingVersion = "2018.2.13p1";
            var realVersion = Version.Parse("2018.2.13.128001");
            Assert.AreEqual(realVersion, Unity.UnityVersion.Parse(marketingVersion));
            Assert.AreEqual(marketingVersion, Unity.UnityVersion.VersionToString(realVersion));
        }
        
        [Test]
        public void VersionToStringTest()
        {
            var marketingVersion = "2018.2.13";
            var realVersion = Version.Parse("2018.2.13.0");
            
            Assert.AreEqual(marketingVersion, Unity.UnityVersion.VersionToString(realVersion));
        }

        [Test]
        public void SortTest()
        {
            var mVersions = new[] {"2018.2.13a20",  "2018.2.13b1", "2018.2.13b3", "2018.2.13b10", "2018.2.13b20", "2018.2.13f1","2018.2.13f20", "2018.2.13p1", "2018.2.13p20"};
            var expected = new[] {"2018.2.13", "2018.2.13a20",  "2018.2.13b1", "2018.2.13b3", "2018.2.13b10", "2018.2.13b20", "2018.2.13f1","2018.2.13f20", "2018.2.13p1", "2018.2.13p20"};
            var actualVersions = mVersions.Select(Unity.UnityVersion.Parse).ToList();
            actualVersions.Add(Version.Parse("2018.2.13"));
            actualVersions = actualVersions.OrderBy().ToList();
            var actual = actualVersions.Select(Unity.UnityVersion.VersionToString).ToArray(); // Select preserves Ordering
            Assert.AreEqual(expected, actual);
        }
    }
}