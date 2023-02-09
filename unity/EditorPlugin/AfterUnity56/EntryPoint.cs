using System;
using JetBrains.Annotations;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
  // DO NOT CHANGE NAME OR NAMESPACE!
  // Accessed from the package via reflection
  // This class is only InitializeOnLoad when the plugin is loaded by Unity from the Assets folder. When the package
  // explicitly loads the plugin from the product install folder, it will execute this class constructor.
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

      // Make sure the PluginEntryPoint class constructor has been called. This used to happen implicitly when this
      // class accessed fields of PluginEntryPoint. Refactorings mean these fields are no longer there, so let's be
      // explicit about the dependency.
      PluginEntryPoint.EnsureInitialised();

      UnityEditorProtocol.OnModelInitialization += Initialization.OnModelInitializationHandler;
      UnityEditorProtocol.OnModelInitialization += Navigation.Initialization.OnModelInitializationHandler;
      UnityEditorProtocol.OnModelInitialization += Packages.Initialization.OnModelInitializationHandler;
      AppDomain.CurrentDomain.DomainUnload += (_, __) =>
      {
        UnityEditorProtocol.OnModelInitialization -= Initialization.OnModelInitializationHandler;
        UnityEditorProtocol.OnModelInitialization -= Navigation.Initialization.OnModelInitializationHandler;
        UnityEditorProtocol.OnModelInitialization -= Packages.Initialization.OnModelInitializationHandler;
      };
    }
  }
}