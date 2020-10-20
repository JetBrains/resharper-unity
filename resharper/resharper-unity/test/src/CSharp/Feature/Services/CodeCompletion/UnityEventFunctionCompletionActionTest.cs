using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle.Settings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Explicit)]
    public class UnityEventFunctionCompletionActionTest : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"CSharp\CodeCompletion\Action";

        // Test what happens when the user types e.g. `void OnAnim{caret}` without an accessibility modifier. The result
        // depends on the code style setting for default private modifier. Default for the test class is explicit
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier01() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier02() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier03() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier04() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier05() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier06() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier07() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier08() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier09() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier10() { DoNamedTest(); }
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void ImplicitAccessibilityModifier11() { DoNamedTest(); }

        [Test] public void ExplicitAccessibilityModifier01() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier02() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier03() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier04() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier05() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier06() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier07() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier08() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier09() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier10() { DoNamedTest(); }
        [Test] public void ExplicitAccessibilityModifier11() { DoNamedTest(); }

        [Test] public void GeneratedCodeResolvesNamespaceGlobally() { DoNamedTest(); }
        [Test] public void RetypeNameOnExistingMethod() { DoNamedTest(); }
        [Test] public void RetypeNameOnExistingMethodWithDifferentSignature() { DoNamedTest(); }
        [Test] public void RetypeNameOnExistingBrokenMethod() { DoNamedTest(); }
        [Test] public void RetypeNameOnExistingMethodWithDifferentArgs() { DoNamedTest(); }
        [Test] public void DoNotRenameNextDeclaration() { DoNamedTest(); }
        [Test] public void EmptyPrefix() { DoNamedTest(); }
    }
}