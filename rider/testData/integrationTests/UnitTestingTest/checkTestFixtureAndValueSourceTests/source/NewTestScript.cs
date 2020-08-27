using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture("Test1Fixture1")]
    [TestFixture("Test1Fixture2")]
    public class Test1
    {
        public Test1(string a) {}

        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses([ValueSource(nameof(_values))] string v)
        {
            Assert.True(v.Contains("b"));
            yield return null;
        }

        private static string[] _values = {"ab", "b"};
    }

    [TestFixture("Test2Fixture1")]
    [TestFixture("Test2Fixture2")]
    public class Test2
    {
        public Test2(string a)
        {
        }

        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            yield return null;
        }
    }

    public class Test3
    {
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses([ValueSource(nameof(_values))] string v)
        {
            Assert.True(v.Contains("b"));
            yield return null;
        }

        private static string[] _values = {"ab", "b"};
    }
}