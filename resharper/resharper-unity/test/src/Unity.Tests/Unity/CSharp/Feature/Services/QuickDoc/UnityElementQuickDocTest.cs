using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.QuickDoc
{
    [TestUnity]
    public class UnityElementQuickDocTest : QuickDocTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\QuickDoc";

        protected override void TestAdditionalInfo(IDeclaredElement declaredElement, IProjectFile projectFile)
        {
        }

        [Test, UnityPluginBackendChecklist("Tooltips and documentation", "Quick doc for Unity elements")] public void EventFunctionQuickDoc() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Tooltips and documentation", "Quick doc for Unity elements")] public void ParameterQuickDoc() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Tooltips and documentation", "Quick doc for Unity elements")] public void SerialisedFieldTooltipQuickDoc() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Tooltips and documentation", "Quick doc for Unity elements")] public void XmlDocOverrides() { DoNamedTest(); }
    }
}