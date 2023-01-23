using NUnit.Framework;
using JetBrains.ReSharper.Daemon.CSharp.Errors;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class SuppressDotsPartialTypeWithSingleUsageWarningTest : CSharpHighlightingTestBase<PartialTypeWithSinglePartWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\Dots";

        [Test]
        public void TestISystemSinglePartialClass()
        {
            DoNamedTest2("DotsClasses.cs");
        }

        [Test]
        public void TestSystemBaseSinglePartialClass()
        {
            DoNamedTest2("DotsClasses.cs");
        }

        [Test]
        public void TestIAspectSinglePartialClass()
        {
            DoNamedTest2("DotsClasses.cs");
        }
    }
}