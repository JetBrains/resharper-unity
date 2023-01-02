using JetBrains.ReSharper.Daemon.CSharp.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public partial class
        SuppressDotsSuperTypeDeclaredInOtherPartWarningTests : CSharpHighlightingTestBase<
            SuperTypeDeclaredInOtherPartWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\Dots";

        [Test]
        public void TestISystemPartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }

        [Test]
        public void TestSystemBasePartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }

        [Test]
        public void TestNegativeSystemBasePartialClassRedundantBaseClass()
        {
            DoNamedTest2($"{TestMethodName2}.part1.cs", $"{TestMethodName2}.Generated.cs", "DotsClasses.cs");
        }
    }
}
