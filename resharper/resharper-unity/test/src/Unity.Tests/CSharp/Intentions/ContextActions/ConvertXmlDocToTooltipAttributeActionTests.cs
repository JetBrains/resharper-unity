using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class ConvertXmlDocToTooltipAttributeAvailabilityTests
        : ContextActionAvailabilityAfterSwaTestBase<ConvertXmlDocToTooltipAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ConvertXmlDocToTooltipAttribute";

        [Test] public void TestAvailability() { DoNamedTest2(); }
    }

    [TestUnity]
    public class ConvertXmlDocToTooltipAttributeActionTests
        : ContextActionExecuteAfterSwaTestBase<ConvertXmlDocToTooltipAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ConvertXmlDocToTooltipAttribute";

        [Test] public void TestConvertSerialisedFieldWithXmlDoc01() { DoNamedTest2(); }
        [Test] public void TestConvertSerialisedFieldWithXmlDoc02() { DoNamedTest2(); }
        [Test] public void TestConvertSerialisedFieldWithXmlDoc03() { DoNamedTest2(); }
        [Test] public void TestConvertSerialisedFieldWithXmlDoc04() { DoNamedTest2(); }
        [Test] public void TestConvertSerialisedFieldWithXmlDoc05() { DoNamedTest2(); }
    }
}