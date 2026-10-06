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

         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void SimpleTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void SimpleNewNameTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void MultiLineCacheTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void MultiLineCacheConflictTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void MultiLineCacheConflictTest2() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void LambdaTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void InlinedCacheTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void OnlyCacheTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void IfTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void SwitchTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void LoopTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void ReturnTest() { DoNamedTest(); }
         [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Cache property value")] public void InlinedRestoreTest() { DoNamedTest(); }
     }
 }