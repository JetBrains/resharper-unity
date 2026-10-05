using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

// ReSharper disable once CheckNamespace
namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
  // DO NOT CHANGE NAME OR NAMESPACE!
  // Accessed from the package via reflection
  // 
  // NOTE: this is a legacy entry point, only used by Unity Rider package before version 3.1.1
  //  Newer versions call PluginEntryPoint.Initialize directly and manage the lifetime on their side.
  //  It will be removed at some point in the future.
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