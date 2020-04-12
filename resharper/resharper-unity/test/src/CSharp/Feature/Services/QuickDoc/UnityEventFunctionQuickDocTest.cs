using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.QuickDoc
{
    [TestUnity]
    public class UnityEventFunctionQuickDocTest : QuickDocTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\QuickDoc";

        protected override void TestAdditionalInfo(IDeclaredElement declaredElement, IProjectFile projectFile)
        {
        }

        [Test] public void EventFunctionQuickDoc() { DoNamedTest(); }
        [Test] public void ParameterQuickDoc() { DoNamedTest(); }
        [Test] public void XmlDocOverrides() { DoNamedTest(); }
    }
}