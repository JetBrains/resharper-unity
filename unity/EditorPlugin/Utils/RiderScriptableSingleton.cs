using JetBrains.Annotations;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
  // no need to set cache, because otherwise new Unity process will restore the value from the file cache.
  //[Location("JetBrainsRiderPluginCache.txt", LocationAttribute.Location.LibraryFolder)]
  internal class RiderScriptableSingleton: ScriptObjectSingleton<RiderScriptableSingleton>
  {
    [SerializeField] private string myPluginVersionUsedToGenerateSolution;
    [SerializeField] private bool myHasModifiedScriptAssets;

    [CanBeNull]
    public string PluginVersionUsedToGenerateSolution
    {
      get => myPluginVersionUsedToGenerateSolution;
      set
      {
        myPluginVersionUsedToGenerateSolution = value;
        Save(true);
      }
    }

    public bool HasModifiedScriptAssets
    {
      get => myHasModifiedScriptAssets;
      set
      {
        myHasModifiedScriptAssets = value;
        Save(true);
      }
    }
  }
}