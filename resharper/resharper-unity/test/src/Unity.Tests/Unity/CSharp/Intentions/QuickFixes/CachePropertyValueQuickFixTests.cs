using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
 {
     [TestUnity]
     [TestCustomInspectionSeverity("Unity.InefficientPropertyAccess", Severity.WARNING)]
     public class CachePropertyValueQuickFixTests : QuickFixTestBase<CachePropertyValueQuickFix>
     {
         protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\CachePropertyValue";
         protected override bool AllowHighlightingOverlap => true;

         [Test] public void SimpleTest() { DoNamedTest(); }
         [Test] public void SimpleNewNameTest() { DoNamedTest(); }
         [Test] public void MultiLineCacheTest() { DoNamedTest(); }
         [Test] public void MultiLineCacheConflictTest() { DoNamedTest(); }
         [Test] public void MultiLineCacheConflictTest2() { DoNamedTest(); }
         [Test] public void LambdaTest() { DoNamedTest(); }
         [Test] public void InlinedCacheTest() { DoNamedTest(); }
         [Test] public void OnlyCacheTest() { DoNamedTest(); }
         [Test] public void IfTest() { DoNamedTest(); }
         [Test] public void SwitchTest() { DoNamedTest(); }
         [Test] public void LoopTest() { DoNamedTest(); }
         [Test] public void ReturnTest() { DoNamedTest(); }
         [Test] public void InlinedRestoreTest() { DoNamedTest(); }
     }
 }