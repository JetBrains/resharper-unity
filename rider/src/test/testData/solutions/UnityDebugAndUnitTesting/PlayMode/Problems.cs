using NUnit.Framework;

// RIDER-46658 Rider does not run PlayMode tests when ValueSource is combined with parameterized TestFixture
namespace Tests
{
    [TestFixture("FixtureTypeA")]
    [TestFixture("FixtureTypeB")]
    public class Problem
    {
        private static string[] _samples =
        {
            "SampleA",
            "SampleB"
        };

        public Problem(string fixtureType)
        {
        }

        [Test]
        public void RunsInUnityAndRider()
        {
        }

        [Test]
        public void RunsInUnityButNotRider([ValueSource(nameof(_samples))] string sample)
        {
        }
    }
}