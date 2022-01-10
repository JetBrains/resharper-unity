using System;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.VersionUtils;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.UnityEditorIntegration.VersionUtils
{
    [TestFixture]
    public class VersionExTests
    {
        [Test]
        public void CompareToLenient_MatchingMajorMinorComponents()
        {
            var version = new Version(2020, 2);

            Assert.AreEqual(0, version.CompareToLenient(new Version(2020, 2)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 3)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 1)));
        }

        [Test]
        public void CompareToLenient_MatchingMajorMinorBuildComponents()
        {
            var version = new Version(2020, 2, 100);

            Assert.AreEqual(0, version.CompareToLenient(new Version(2020, 2, 100)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 2, 101)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 2, 99)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 3, 100)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 1, 100)));
        }

        [Test]
        public void CompareToLenient_MatchingMajorMinorBuildRevisionComponents()
        {
            var version = new Version(2020, 2, 100, 42);

            Assert.AreEqual(0, version.CompareToLenient(new Version(2020, 2, 100, 42)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 2, 100, 43)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 2, 100, 41)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 3, 100, 42)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 1, 100, 42)));
        }

        [Test]
        public void CompareToLenient_OtherHasMoreComponents()
        {
            var version = new Version(2020, 2);

            Assert.AreEqual(0, version.CompareToLenient(new Version(2020, 2, 100)));
            Assert.AreEqual(0, version.CompareToLenient(new Version(2020, 2, 0)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 3, 100)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 1, 100)));
        }

        [Test]
        public void CompareToLenient_OtherHasLessComponents()
        {
            var version = new Version(2020, 2, 1);

            Assert.AreEqual(0, version.CompareToLenient(new Version(2020, 2)));
            Assert.AreEqual(-1, version.CompareToLenient(new Version(2020, 3)));
            Assert.AreEqual(1, version.CompareToLenient(new Version(2020, 1)));
        }
    }
}