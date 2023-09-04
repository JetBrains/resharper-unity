using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

// RIDER-48686 Rider Test runner ignores [Explicit] Attribute
namespace Tests
{
    public class NewTestScript
    {
        [Test]
        public void NewTestScriptSimplePasses()
        {
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            yield return null;
            Assert.Pass();
        }

        [Test]
        [Explicit]
        public void NewTestScriptSimplePassesExplicit()
        {
            Assert.Fail();
        }

        [UnityTest]
        [Explicit]
        public IEnumerator NewTestScriptWithEnumeratorPassesExplicit()
        {
            yield return null;
            Assert.Fail();
        }
    }
}