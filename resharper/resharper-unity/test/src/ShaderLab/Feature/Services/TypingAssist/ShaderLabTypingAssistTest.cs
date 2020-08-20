using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.FeaturesTestFramework.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Feature.Services.TypingAssist
{
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
        [Test] public void SmartEnterHlsl01() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl02() { DoNamedTest(); }
        [Test] public void SmartEnterHlsl03() { DoNamedTest(); }

        [Test] public void SmartBackspace01() { DoNamedTest(); }
        [Test] public void SmartBackspace02() { DoNamedTest(); }
        [Test] public void SmartBackspace03() { DoNamedTest(); }
        
        
        // There is no smart backspace in C++ now, that test should fail when cpp team will implement it
        // we should check how injects are affected
        [Test] public void SmartBackspaceCppIsNotWorking() { DoNamedTest(); }
        
        protected override void DoTest(Lifetime lifetime)
        {
            var settingsStore = ChangeSettingsTemporarily(TestLifetime).BoundStore;
            settingsStore.SetValue((ShaderLabFormatSettingsKey key) => key.INDENT_SIZE, 4);
            
            base.DoTest(lifetime);
        }
    }
}