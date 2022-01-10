using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.VersionUtils;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [TestFixture]
    public class JetSemanticVersionRangeTests
    {
        [Test]
        public void TestExactVersion()
        {
            Assert.True(JetSemanticVersionRange.TryParse("[1.2.3]", out var range));
            Assert.NotNull(range);
            Assert.AreEqual("x = 1.2.3", range.ToString());
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.2.2")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.2.3")));
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.2.4")));
        }

        [Test]
        public void TestThisVersionOrLater()
        {
            Assert.True(JetSemanticVersionRange.TryParse("1.2.3", out var range));
            Assert.NotNull(range);
            Assert.AreEqual("x >= 1.2.3", range.ToString());
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.2.2")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.2.3")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.2.4")));
        }

        [Test]
        public void TestInclusiveRange()
        {
            Assert.True(JetSemanticVersionRange.TryParse("[1.2.3,1.4.5]", out var range));
            Assert.NotNull(range);
            Assert.AreEqual("1.2.3 <= x <= 1.4.5", range.ToString());
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.2.2")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.2.3")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.2.4")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.4.5")));
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.4.6")));
        }

        [Test]
        public void TestExclusiveRange()
        {
            Assert.True(JetSemanticVersionRange.TryParse("(1.2.3,1.4.5)", out var range));
            Assert.NotNull(range);
            Assert.AreEqual("1.2.3 < x < 1.4.5", range.ToString());
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.2.2")));
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.2.3")));
            Assert.True(range.IsValid(JetSemanticVersion.Parse("1.2.4")));
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.4.5")));
            Assert.False(range.IsValid(JetSemanticVersion.Parse("1.4.6")));
        }

        [Test]
        public void CanParseVersionRangeFromInclusive()
        {
            Assert.True(JetSemanticVersionRange.TryParse("[1.2.3,1.4.5)", out var range));
            Assert.NotNull(range);
            Assert.AreEqual("1.2.3 <= x < 1.4.5", range.ToString());
        }

        [Test]
        public void CanParseVersionRangeToInclusive()
        {
            Assert.True(JetSemanticVersionRange.TryParse("(1.2.3,1.4.5]", out var range));
            Assert.NotNull(range);
            Assert.AreEqual("1.2.3 < x <= 1.4.5", range.ToString());
        }

        [Test]
        public void SpacesNotAllowedInExpression()
        {
            Assert.False(JetSemanticVersionRange.TryParse("[1.2.3, 1.4.5]", out var range));
            Assert.Null(range);
        }

        [Test]
        public void TestMustHaveBalancedBrackets1()
        {
            Assert.False(JetSemanticVersionRange.TryParse("[1.2.3,1.4.5", out var range));
            Assert.Null(range);
        }

        [Test]
        public void TestMustHaveBalancedBrackets2()
        {
            Assert.False(JetSemanticVersionRange.TryParse("(1.2.3,1.4.5", out var range));
            Assert.Null(range);
        }

        [Test]
        public void TestMustHaveBalancedBrackets3()
        {
            Assert.False(JetSemanticVersionRange.TryParse("1.2.3,1.4.5]", out var range));
            Assert.Null(range);
        }

        [Test]
        public void TestMustHaveBalancedBrackets4()
        {
            Assert.False(JetSemanticVersionRange.TryParse("1.2.3,1.4.5)", out var range));
            Assert.Null(range);
        }
    }
}