using System;
using JetBrains.Annotations;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
  // DO NOT CHANGE NAME OR NAMESPACE!
  // Accessed from the package via reflection
  [InitializeOnLoad, PublicAPI]
  public static class EntryPoint
  {
    // DO NOT REMOVE OR REFACTOR!
    // When loaded by Unity from the Assets folder, Unity will automatically run this static class constructor. When the
    // package loads the type, it will explicitly invoke the class constructor
    [PublicAPI]
    static EntryPoint()
    {
      if (UnityUtils.IsInBatchModeAndNotInRiderTests)
        return;

      PluginEntryPoint.OnModelInitialization += Initialization.OnModelInitializationHandler;
      PluginEntryPoint.OnModelInitialization += Navigation.Initialization.OnModelInitializationHandler;
      PluginEntryPoint.OnModelInitialization += Packages.Initialization.OnModelInitializationHandler;
      AppDomain.CurrentDomain.DomainUnload += (_, __) =>
      {
        PluginEntryPoint.OnModelInitialization -= Initialization.OnModelInitializationHandler;
        PluginEntryPoint.OnModelInitialization -= Navigation.Initialization.OnModelInitializationHandler;
        PluginEntryPoint.OnModelInitialization -= Packages.Initialization.OnModelInitializationHandler;
      };
    }
  }
}