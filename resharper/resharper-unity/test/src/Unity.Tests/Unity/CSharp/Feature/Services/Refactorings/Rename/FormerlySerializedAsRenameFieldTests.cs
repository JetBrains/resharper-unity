using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    [TestUnity]
    public class FormerlySerializedAsRenameFieldTests : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Refactorings\Rename";

        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("References and rename", "Rename serialized field with FormerlySerializedAs")] public void Test07() { DoNamedTest(); }
    }
}