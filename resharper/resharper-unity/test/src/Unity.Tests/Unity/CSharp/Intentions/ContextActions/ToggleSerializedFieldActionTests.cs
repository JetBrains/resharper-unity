using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class ToggleSerializedFieldActionAvailabilityTest
        : ContextActionAvailabilityTestBase<ToggleSerializedFieldAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"ToggleSerializedField";

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class ToggleSerializedFieldActionExecutionTest
        : ContextActionExecuteTestBase<ToggleSerializedFieldAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ToggleSerializedField";

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToNonSerialized01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToNonSerialized02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToNonSerialized03() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToNonSerialized04() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToNonSerialized05() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerialized01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerialized02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerialized03() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveReadonly01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveReadonly02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveReadonly03() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveStatic01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveStatic02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveStatic03() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveReadonlyStatic01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveReadonlyStatic02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field")] public void TestToSerializedRemoveReadonlyStatic03() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToNonSerialized01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToNonSerialized02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToNonSerialized03() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToNonSerialized04() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToNonSerialized05() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerialized01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerialized02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerialized03() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerializedRemoveReadonly01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerializedRemoveReadonly02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerializedRemoveStatic01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerializedRemoveStatic02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerializedRemoveReadonlyStatic01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for a single declarator")] public void TestJustOneToSerializedRemoveReadonlyStatic02() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToNonSerialized01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToNonSerialized02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToNonSerialized03() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToNonSerialized04() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToNonSerialized05() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerialized01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerialized02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerialized03() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerializedRemoveReadonly01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerializedRemoveReadonly02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerializedRemoveStatic01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerializedRemoveStatic02() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerializedRemoveReadonlyStatic01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Toggle serialized field for all declarators")] public void TestAllToSerializedRemoveReadonlyStatic02() { DoNamedTest2(); }
    }
}