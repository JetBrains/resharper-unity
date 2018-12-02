using System;
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
            var realVersion = Version.Parse("2018.2.13.1121");
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
    }
}