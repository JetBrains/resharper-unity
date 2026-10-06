using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
     [TestUnity]
    public class RedundantAttributeOnTargetAnalyzerTests : CSharpHighlightingTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\RedundantAttributeOnTarget";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestAddComponentMenu() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestExecuteInEditMode() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestHideInInspector() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestImageEffectAllowedInSceneView() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestImageEffectOpaque() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestImageEffectTransformsToLDR() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestSerializeField() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestCanEditMultipleObjects() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestCustomEditor() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestDrawGizmo() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestDidReloadScripts() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestOnOpenAssetAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestPostProcessBuildAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestPostProcessSceneAttribute() { DoNamedTest2(); }
    }

    [TestUnity]
    public class RedundantAttributeOnTargetGlobalStageAnalyzerTests : UnitySerializationGlobalStageTestBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\RedundantAttributeOnTarget";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestAddComponentMenu() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestExecuteInEditMode() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestHideInInspector() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestImageEffectAllowedInSceneView() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestImageEffectOpaque() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestImageEffectTransformsToLDR() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestSerializeField() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestCanEditMultipleObjects() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestCustomEditor() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestDrawGizmo() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestDidReloadScripts() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestOnOpenAssetAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestPostProcessBuildAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "Attributes", "Redundant attribute on target")] public void TestPostProcessSceneAttribute() { DoNamedTest2(); }
    }
}