using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class ToggleSerializedFieldActionAvailabilityTest
        : ContextActionAvailabilityTestBase<ToggleSerializedFieldAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"ToggleSerializedField";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class ToggleSerializedFieldActionExecutionTest
        : ContextActionExecuteTestBase<ToggleSerializedFieldAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "ToggleSerializedField";

        [Test] public void TestToNonSerialized01() { DoNamedTest2(); }
        [Test] public void TestToNonSerialized02() { DoNamedTest2(); }
        [Test] public void TestToNonSerialized03() { DoNamedTest2(); }
        [Test] public void TestToNonSerialized04() { DoNamedTest2(); }
        [Test] public void TestToNonSerialized05() { DoNamedTest2(); }

        [Test] public void TestToSerialized01() { DoNamedTest2(); }
        [Test] public void TestToSerialized02() { DoNamedTest2(); }
        [Test] public void TestToSerialized03() { DoNamedTest2(); }

        [Test] public void TestToSerializedRemoveReadonly01() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveReadonly02() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveReadonly03() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveStatic01() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveStatic02() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveStatic03() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveReadonlyStatic01() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveReadonlyStatic02() { DoNamedTest2(); }
        [Test] public void TestToSerializedRemoveReadonlyStatic03() { DoNamedTest2(); }

        [Test] public void TestJustOneToNonSerialized01() { DoNamedTest2(); }
        [Test] public void TestJustOneToNonSerialized02() { DoNamedTest2(); }
        [Test] public void TestJustOneToNonSerialized03() { DoNamedTest2(); }
        [Test] public void TestJustOneToNonSerialized04() { DoNamedTest2(); }
        [Test] public void TestJustOneToNonSerialized05() { DoNamedTest2(); }

        [Test] public void TestJustOneToSerialized01() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerialized02() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerialized03() { DoNamedTest2(); }

        [Test] public void TestJustOneToSerializedRemoveReadonly01() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerializedRemoveReadonly02() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerializedRemoveStatic01() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerializedRemoveStatic02() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerializedRemoveReadonlyStatic01() { DoNamedTest2(); }
        [Test] public void TestJustOneToSerializedRemoveReadonlyStatic02() { DoNamedTest2(); }

        [Test] public void TestAllToNonSerialized01() { DoNamedTest2(); }
        [Test] public void TestAllToNonSerialized02() { DoNamedTest2(); }
        [Test] public void TestAllToNonSerialized03() { DoNamedTest2(); }
        [Test] public void TestAllToNonSerialized04() { DoNamedTest2(); }
        [Test] public void TestAllToNonSerialized05() { DoNamedTest2(); }

        [Test] public void TestAllToSerialized01() { DoNamedTest2(); }
        [Test] public void TestAllToSerialized02() { DoNamedTest2(); }
        [Test] public void TestAllToSerialized03() { DoNamedTest2(); }

        [Test] public void TestAllToSerializedRemoveReadonly01() { DoNamedTest2(); }
        [Test] public void TestAllToSerializedRemoveReadonly02() { DoNamedTest2(); }
        [Test] public void TestAllToSerializedRemoveStatic01() { DoNamedTest2(); }
        [Test] public void TestAllToSerializedRemoveStatic02() { DoNamedTest2(); }
        [Test] public void TestAllToSerializedRemoveReadonlyStatic01() { DoNamedTest2(); }
        [Test] public void TestAllToSerializedRemoveReadonlyStatic02() { DoNamedTest2(); }
    }
}