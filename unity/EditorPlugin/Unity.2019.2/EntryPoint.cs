using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

// ReSharper disable once CheckNamespace
namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
  // DO NOT CHANGE NAME OR NAMESPACE!
  // Accessed from the package via reflection
  // When the package explicitly loads the plugin from the product install folder, it will execute this class constructor.
  [PublicAPI]
  public static class EntryPoint
  {
    // DO NOT REMOVE OR REFACTOR!
    // The package explicitly invokes it via reflection.
    [PublicAPI] static EntryPoint()
    {
      var lifetimeDefinition = Lifetime.Define(Lifetime.Eternal);
      AppDomain.CurrentDomain.DomainUnload += (_, __) =>
      {
        Log.GetLog("RiderPlugin").Verbose("AppDomain.CurrentDomain.DomainUnload lifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      };
      
      PluginEntryPoint.Initialize(lifetimeDefinition.Lifetime);
    }
  }
}