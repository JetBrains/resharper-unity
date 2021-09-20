using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Psi.Resolve
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferenceTests : AsmDefReferenceTestsBase<AsmDefNameReference>
    {
        [Test] public void TestUnresolvedReference01() { DoNamedTest2(); }
        [Test] public void TestUnresolvedReference02() { DoNamedTest2("UnresolvedReference02_SecondProject.asmdef"); }
        [Test] public void TestCrossProjectReference() { DoNamedTest2("CrossProjectReference_SecondProject.asmdef");}
        [Test] public void TestCorrectJsonReferences() { DoNamedTest2(); }
        // This isn't exactly like production, because it adds the .meta file to the project, while the external files
        // module is disabled for tests
        [Test] public void TestGuidReference() { DoNamedTest2("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }
}
