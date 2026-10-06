using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class PropertyDrawerOnGUIBaseCallWarningTests : CSharpHighlightingTestBase<PropertyDrawerOnGUIBaseWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Event functions", "PropertyDrawer OnGUI base call")] public void TestPropertyDrawerOnGUIBaseCallWarning() { DoNamedTest2(); }
    }
}