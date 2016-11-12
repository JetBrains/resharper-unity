using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.Descriptions
{
    [TestUnity]
    public class IdentifierTooltipTest : IdentifierTooltipTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\IdentifierTooltip";

        [Test] public void Message01() { DoNamedTest(); }
        [Test] public void MessageParameter01() { DoNamedTest(); }
    }
}