using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Features.ReSpeller.ReSharperSpecific.QuickFixes;
using JetBrains.ReSharper.Features.ReSpeller.Settings;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.FeaturesTestFramework.SpellEngineStub;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.ShaderLab.ReSpeller;

[TestFixture, Category("ReSpeller")]
[TestFileExtension(".shader")]
public class ShaderLabQuickFixTests : QuickFixTestBase<TypoQuickFix>
{
  private LifetimeDefinition mySettingsStoreLifetimeDefinition = new();
  private readonly double mySettingsStorePriority = ProjectModelSettingsStorageMountPointPriorityClasses.ConfigFiles * 0.99d;
  
  public override void TestFixtureSetUp()
  {
    base.TestFixtureSetUp();
    var testSpellService = ShellInstance.GetComponent<TestSpellService>();
    testSpellService.AddWrongPhrasePart(TestFixtureLifetime, "Colgor", "Color");
    testSpellService.AddWrongWord(TestFixtureLifetime, "colgor", "color");
    testSpellService.AddWrongWord(TestFixtureLifetime, "opafque", "opaque");
    mySettingsStoreLifetimeDefinition = new LifetimeDefinition();
    var settingsStore = ChangeSettingsTemporarily(mySettingsStoreLifetimeDefinition.Lifetime, mySettingsStorePriority).BoundStore;
    RunGuarded(() => settingsStore.SetValue(GrammarAndSpellingSettingsAccessor.CheckGrammarInInterpolatedStringLiterals, true));
  }

  public override void TestFixtureTearDown()
  {
    RunGuarded(() => mySettingsStoreLifetimeDefinition.Terminate());
    base.TestFixtureTearDown();
  }

  protected override string RelativeTestDataPath => @"ShaderLab\ReSpeller\QuickFixes";

  [Test] public void TestShaderComments() => DoNamedTest2();
  [Test] public void TestShaderName() => DoNamedTest2();
  [Test] public void TestShaderProperties() => DoNamedTest2();
  [Test] public void TestSubshaderTags() => DoNamedTest2();
}