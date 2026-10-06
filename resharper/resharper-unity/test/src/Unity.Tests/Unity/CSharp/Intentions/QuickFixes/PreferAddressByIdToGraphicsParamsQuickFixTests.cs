using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferAddressByIdToGraphicsParamsQuickFixAvailabilityTests : CSharpHighlightingTestBase<PreferAddressByIdToGraphicsParamsWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferAddressByIdToGraphicsParams\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void NameOfTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void LocalConstantTest() { DoNamedTest(); }
    }

    [TestUnity]
    public class PreferAddressByIdToGraphicsParamsQuickFixTests : QuickFixTestBase<PreferAddressByIdToGraphicsParamsQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferAddressByIdToGraphicsParams";
        protected override bool AllowHighlightingOverlap => true;

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void SimpleTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void UnderscoreNameTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void NewNameTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ReuseTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ReuseFailedCreateNewTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ReuseConflictNameTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void NestedClassTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void WithoutUnityNamespaceTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void InvalidLiteralForPropertyNameTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ShaderPropertyTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void AnimatorPropertyTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ConstantValueReuseTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ConstantValueConcatReuseTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void PropertyReuseTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void StructTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void NestedReuseTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ConstConcatCreateFieldTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void PartialClassTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void UniqueNameTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void ComputeShaderPropertyTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void CommandBufferPropertyTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer address by id for graphics params")] public void MaterialPropertyTest() { DoNamedTest(); }
    }
}