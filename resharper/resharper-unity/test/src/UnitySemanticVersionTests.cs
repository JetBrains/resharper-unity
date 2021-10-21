using JetBrains.ReSharper.Plugins.Unity.Utils;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [TestFixture]
    public class UnitySemanticVersionTests
    {
        [Test]
        public void TestParsingUnityVersion()
        {
            Assert.True(UnitySemanticVersion.TryParseProductVersion("2020.1.3a1", out var result));
            Assert.AreEqual("2020.1.3a1", result.ToString());
            Assert.AreEqual("2020.1.3-a1", result.SemanticVersion.ToString());
            Assert.AreEqual(2020, result.SemanticVersion.Major);
            Assert.AreEqual(1, result.SemanticVersion.Minor);
            Assert.AreEqual(3, result.SemanticVersion.Patch);
            Assert.AreEqual("a1", result.SemanticVersion.PrereleaseMetadata);
        }

        [Test]
        public void TestParsingUnityVersion2()
        {
            Assert.True(UnitySemanticVersion.TryParseProductVersion("2020.1.3b1", out var result));
            Assert.AreEqual("2020.1.3b1", result.ToString());
            Assert.AreEqual("2020.1.3-b1", result.SemanticVersion.ToString());
            Assert.AreEqual(2020, result.SemanticVersion.Major);
            Assert.AreEqual(1, result.SemanticVersion.Minor);
            Assert.AreEqual(3, result.SemanticVersion.Patch);
            Assert.AreEqual("b1", result.SemanticVersion.PrereleaseMetadata);
        }

        [Test]
        public void TestParsingUnityVersion3()
        {
            Assert.True(UnitySemanticVersion.TryParseProductVersion("2020.1.3f1", out var result));
            Assert.AreEqual("2020.1.3f1", result.ToString());
            Assert.AreEqual("2020.1.3-f1", result.SemanticVersion.ToString());
            Assert.AreEqual(2020, result.SemanticVersion.Major);
            Assert.AreEqual(1, result.SemanticVersion.Minor);
            Assert.AreEqual(3, result.SemanticVersion.Patch);
            Assert.AreEqual("f1", result.SemanticVersion.PrereleaseMetadata);
        }

        [Test]
        public void TestParsingSemanticVersion()
        {
            Assert.True(UnitySemanticVersion.TryParse("1.3.4-pre1", out var result));
            Assert.AreEqual("1.3.4-pre1", result.ToString());
            Assert.AreEqual("1.3.4-pre1", result.SemanticVersion.ToString());
            Assert.AreEqual(1, result.SemanticVersion.Major);
            Assert.AreEqual(3, result.SemanticVersion.Minor);
            Assert.AreEqual(4, result.SemanticVersion.Patch);
            Assert.AreEqual("pre1", result.SemanticVersion.PrereleaseMetadata);
        }

        [Test]
        public void TestParsingNumericMetadata()
        {
            Assert.True(UnitySemanticVersion.TryParse("2021.3.0-9999", out var result));
            Assert.AreEqual("2021.3.0-9999", result.ToString());
            Assert.AreEqual(2021, result.SemanticVersion.Major);
            Assert.AreEqual(3, result.SemanticVersion.Minor);
            Assert.AreEqual(0, result.SemanticVersion.Patch);
            Assert.AreEqual("9999", result.SemanticVersion.PrereleaseMetadata);
        }
    }

    [TestFixture]
    public class UnitySemanticVersionRangeTests
    {
        [Test]
        public void TestParsingUnityProductVersionRangeAtLeast()
        {
            Assert.True(UnitySemanticVersionRange.TryParse("2020.1.3a2", out var result));
            Assert.AreEqual("x >= 2020.1.3a2", result.ToString());
            AssertNotValidVersion("2020.1.2", result);
            AssertNotValidVersion("2020.1.3a1", result);
            AssertValidVersion("2020.1.3a2", result);
            AssertValidVersion("2020.1.3a3", result);
            AssertValidVersion("2020.1.3b1", result);
            AssertValidVersion("2020.1.3f1", result);
            AssertValidVersion("2020.1.3", result);
        }

        [Test]
        public void TestParsingUnityProductVersionRangeExact()
        {
            Assert.True(UnitySemanticVersionRange.TryParse("[2020.1.3a2]", out var result));
            Assert.AreEqual("x = 2020.1.3a2", result.ToString());
            AssertNotValidVersion("2020.1.3a1", result);
            AssertValidVersion("2020.1.3a2", result);
            AssertNotValidVersion("2020.1.3a3", result);
            AssertNotValidVersion("2020.1.3b1", result);
            AssertNotValidVersion("2020.1.3", result);
        }

        [Test]
        public void TestParsingUnityProductVersionRangeInclusive()
        {
            Assert.True(UnitySemanticVersionRange.TryParse("[2020.1.3a2,2020.1.3b2]", out var result));
            Assert.AreEqual("2020.1.3a2 <= x <= 2020.1.3b2", result.ToString());
            AssertNotValidVersion("2020.1.3a1", result);
            AssertValidVersion("2020.1.3a2", result);
            AssertValidVersion("2020.1.3a3", result);
            AssertValidVersion("2020.1.3b1", result);
            AssertValidVersion("2020.1.3b2", result);
            AssertNotValidVersion("2020.1.3", result);
        }

        [Test]
        public void TestParsingUnityProductVersionRangeExclusive()
        {
            Assert.True(UnitySemanticVersionRange.TryParse("(2020.1.3a2,2020.1.3b2)", out var result));
            Assert.AreEqual("2020.1.3a2 < x < 2020.1.3b2", result.ToString());
            AssertNotValidVersion("2020.1.3a1", result);
            AssertNotValidVersion("2020.1.3a2", result);
            AssertValidVersion("2020.1.3a3", result);
            AssertValidVersion("2020.1.3b1", result);
            AssertNotValidVersion("2020.1.3b2", result);
            AssertNotValidVersion("2020.1.3", result);
        }

        private static void AssertValidVersion(string version, UnitySemanticVersionRange range)
        {
            Assert.True(UnitySemanticVersion.TryParseProductVersion(version, out var expected));
            Assert.True(range.IsValid(expected));
        }

        private static void AssertNotValidVersion(string version, UnitySemanticVersionRange range)
        {
            Assert.True(UnitySemanticVersion.TryParseProductVersion(version, out var expected));
            Assert.False(range.IsValid(expected));
        }
    }
}