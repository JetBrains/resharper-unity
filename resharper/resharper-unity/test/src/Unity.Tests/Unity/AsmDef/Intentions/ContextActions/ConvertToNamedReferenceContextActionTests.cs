using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.ContextActions;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Intentions.ContextActions
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ConvertToNamedReferenceActionAvailabilityTests
        : ContextActionAvailabilityTestBase<ConvertToNamedReferenceContextAction>
    {
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ConvertToNamedReference";

        [Test] public void TestAvailability01() { DoNamedTest2("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ConvertToNamedReferenceActionExecutionTests
        : ContextActionExecuteTestBase<ConvertToNamedReferenceContextAction>
    {
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ConvertToNamedReference";

        [Test] public void TestExecute01() { DoNamedTest2("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
        [Test, ExecuteScopedActionInFile] public void TestExecuteInScope() { DoNamedTest2("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }
}