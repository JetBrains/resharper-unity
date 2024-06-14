using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Features.ReSpeller.ReSharperSpecific.Highlightings;
using JetBrains.ReSharper.Features.ReSpeller.Settings;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.SpellEngineStub;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.ShaderLab.ReSpeller
{
  [TestUnity, HighlightOnly(typeof(ReSpellerHighlightingBase))]
  [TestFileExtension(".shader")]
  [TestFixture, Category("ReSpeller")]
  public class ShaderLabSpellHighlightingsTest : ShaderLabHighlightingTestBase<ReSpellerHighlightingBase>
  { 
    protected override void SetupSettings()
    {
      base.SetupSettings();
      SettingsStore.SetValue(GrammarAndSpellingSettingsAccessor.CheckGrammarInInterpolatedStringLiterals, true);
    }

    private static readonly string[] ourMisspelledWords = {
      "Spiell", "textue", "whitte", "Shiney", "Shinins", "Opafque", "Difuse"
    };
      
    public override void TestFixtureSetUp()
    {
      base.TestFixtureSetUp();
      var testSpellService = ShellInstance.GetComponent<TestSpellService>();
      testSpellService.ConfigureDefault(TestFixtureLifetime);
      foreach (var wrongToken in ourMisspelledWords)
      {
        testSpellService.AddWrongWord(TestFixtureLifetime, wrongToken);
      }
    }
    
    protected override string RelativeTestDataPath => @"ShaderLab\ReSpeller\Highlightings";
    
    [TestCase]
    public void TestSpellCheckStringLiterals() => DoNamedTest2();
  }
}
