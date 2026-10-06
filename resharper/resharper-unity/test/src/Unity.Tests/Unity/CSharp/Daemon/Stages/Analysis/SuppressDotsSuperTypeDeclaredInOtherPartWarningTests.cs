using JetBrains.ReSharper.Daemon.CSharp.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class SuppressDotsSuperTypeDeclaredInOtherPartWarningTests : CSharpHighlightingTestBase<SuperTypeDeclaredInOtherPartWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\Dots";

        //.Generated is used to mark file as ISourceGeneratorOutputFile - check DotsElementsSuperTypeDeclaredInOtherPartSuppressorMock

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS inspection suppression")]
        public void TestISystemPartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS inspection suppression")]
        public void TestIJobEntityPartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS inspection suppression")]
        public void TestSystemBasePartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS inspection suppression")]
        public void TestIAspectPartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }

        [Test]
        [UnityPluginBackendChecklist("DOTS and Burst", "DOTS inspection suppression")]
        public void TestNegativeSystemBasePartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.part1.cs", $"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }
    }
}