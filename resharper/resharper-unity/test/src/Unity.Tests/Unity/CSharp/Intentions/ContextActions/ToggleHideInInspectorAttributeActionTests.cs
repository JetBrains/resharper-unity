using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class ToggleHideInInspectorAttributeActionAvailabilityTest
        : ContextActionAvailabilityTestBase<ToggleHideInInspectorAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"ToggleHideInInspectorAttribute";

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class ToggleHideInInspectorAttributeActionExecutionTest
        : ContextActionExecuteTestBase<ToggleHideInInspectorAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ToggleHideInInspectorAttribute";

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestAddAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestAddToExistingAttributes() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestAddAttributeToAllFields() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestAddToOneOfMultipleFields() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestRemoveAttribute01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestRemoveAttribute02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestRemoveAttribute03() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestRemoveAttributeFromAllFields() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle HideInInspector attribute")] public void TestRemoveAttributeFromOneOfMultipleFields() { DoNamedTest2(); }
    }
}