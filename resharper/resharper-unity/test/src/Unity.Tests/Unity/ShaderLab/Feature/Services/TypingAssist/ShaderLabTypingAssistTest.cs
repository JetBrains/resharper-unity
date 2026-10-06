using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.TypingAssist
{
    [RequireHlslSupport]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    [TestSettingsKey(typeof(ShaderLabFormatSettingsKey))]
    public class ShaderLabTypingAssistTest : TypingAssistTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\TypingAssist";

        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter07() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter08() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter09() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter10() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter11() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter12() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter13() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter14() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter15() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter16() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter17() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter18() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter19() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter20() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter21() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter22() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter23() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter24() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter25() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter26() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter27() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter28() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter")] public void SmartEnter29() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl07() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl08() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart enter in HLSL blocks")] public void SmartEnterHlsl09() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart backspace")] public void SmartBackspace01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart backspace")] public void SmartBackspace02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart backspace")] public void SmartBackspace03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: smart backspace")] public void SmartBackspaceHlsl01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartLBrace01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartLBrace02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartLBracket01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartLBracket02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartLParen01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartLParen02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartQuot01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartQuot02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartQuot03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("ShaderLab", "Editing", "Typing assist: brackets and quotes")] public void SmartQuot04() { DoNamedTest(); }

        protected override void DoTest(Lifetime lifetime, ISolution solution)
        {
            var settingsStore = ChangeSettingsTemporarily(TestLifetime).BoundStore;
            settingsStore.SetValue((ShaderLabFormatSettingsKey key) => key.INDENT_SIZE, 4);

            base.DoTest(lifetime, solution);
        }
    }
}