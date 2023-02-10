using System;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
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

      // Make sure the main PluginEntryPoint is initialised. When loaded from the Assets folder, this will happen
      // automatically, since PluginEntryPoint is [InitializeOnLoad]. When loaded from the package, the package will
      // explicitly invoke this class constructor, but nothing else.
      PluginEntryPoint.EnsureInitialised();

      var lifetimeDefinition = Lifetime.Define(Lifetime.Eternal);
      AppDomain.CurrentDomain.DomainUnload += (_, __) => lifetimeDefinition.Terminate();

      // TODO: Move all of this into PluginEntryPoint
      UnityEditorProtocol.Models.View(lifetimeDefinition.Lifetime, (modelLifetime, _, model) =>
      {
        Initialization.Advise(modelLifetime, model);
        Navigation.Initialization.Advise(modelLifetime, model);
#if UNITY_2019_2_OR_NEWER
        Packages.Initialization.Advise(modelLifetime, model);
#endif
      });
    }
  }
}