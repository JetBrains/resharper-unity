using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
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

        [Test] public void SmartEnter01() { DoNamedTest(); }
        [Test] public void SmartEnter02() { DoNamedTest(); }
        [Test] public void SmartEnter03() { DoNamedTest(); }
        [Test] public void SmartEnter04() { DoNamedTest(); }
        [Test] public void SmartEnter05() { DoNamedTest(); }
        [Test] public void SmartEnter06() { DoNamedTest(); }
        [Test] public void SmartEnter07() { DoNamedTest(); }
        [Test] public void SmartEnter08() { DoNamedTest(); }
        [Test] public void SmartEnter09() { DoNamedTest(); }
        [Test] public void SmartEnter10() { DoNamedTest(); }
        [Test] public void SmartEnter11() { DoNamedTest(); }
        [Test] public void SmartEnter12() { DoNamedTest(); }
        [Test] public void SmartEnter13() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl01() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl02() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl03() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl04() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl05() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl06() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl07() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl08() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl09() { DoNamedTest(); }
        [Test] public void SmartBackspace01() { DoNamedTest(); }
        [Test] public void SmartBackspace02() { DoNamedTest(); }
        [Test] public void SmartBackspace03() { DoNamedTest(); }
        [Test] public void SmartBackspaceHlsl01() { DoNamedTest(); }
        [Test] public void SmartLBrace01() { DoNamedTest(); }
        [Test] public void SmartLBrace02() { DoNamedTest(); }
        [Test] public void SmartLBracket01() { DoNamedTest(); }
        [Test] public void SmartLBracket02() { DoNamedTest(); }
        [Test] public void SmartLParen01() { DoNamedTest(); }
        [Test] public void SmartLParen02() { DoNamedTest(); }
        [Test] public void SmartQuot01() { DoNamedTest(); }
        [Test] public void SmartQuot02() { DoNamedTest(); }
        [Test] public void SmartQuot03() { DoNamedTest(); }
        [Test] public void SmartQuot04() { DoNamedTest(); }

        protected override void DoTest(Lifetime lifetime)
        {
            var settingsStore = ChangeSettingsTemporarily(TestLifetime).BoundStore;
            settingsStore.SetValue((ShaderLabFormatSettingsKey key) => key.INDENT_SIZE, 4);

            base.DoTest(lifetime);
        }
    }
}