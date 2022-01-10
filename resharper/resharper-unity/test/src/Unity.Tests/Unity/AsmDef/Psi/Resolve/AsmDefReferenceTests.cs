using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Psi.Resolve
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferenceTests : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Psi\Resolve";
        protected override bool AcceptReference(IReference reference) => reference is AsmDefNameReference;

        [Test] public void TestCorrectJsonReferences() { DoNamedTest2(); }
        [Test] public void TestUnresolvedReference01() { DoNamedTest2(); }

        [Test]
        public void TestUnresolvedReference02()
        {
            DoTestSolution(new[] { "UnresolvedReference02.asmdef" },
                new[] { "UnresolvedReference02_SecondProject.asmdef" });
        }

        [Test]
        public void TestCrossProjectReference()
        {
            DoTestSolution(new[] { "CrossProjectReference.asmdef" },
                new[] { "CrossProjectReference_SecondProject.asmdef" });
        }

        [Test]
        public void TestGuidReference()
        {
            // This isn't exactly like a production Unity project, because we're adding the .meta file directly, but the
            // external files module isn't available in tests
            DoTestSolution(new[] { "GuidReference.asmdef" },
                new[] { "GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta" });
        }
    }
}
