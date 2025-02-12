#if UNITY_2019_2
using JetBrains.Annotations;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
  // no need to set cache, because otherwise new Unity process will restore the value from the file cache.
  //[Location("JetBrainsRiderPluginCache.txt", LocationAttribute.Location.LibraryFolder)]

  // DO NOT CHANGE NAME OR NAMESPACE!
  // Called from package via reflection. See also ScriptObjectSingleton.Instance
  [PublicAPI]
  internal class RiderScriptableSingleton: ScriptObjectSingleton<RiderScriptableSingleton>
  {
    [SerializeField]
    // ReSharper disable once InconsistentNaming
    private bool myCsprojProcessedOnce;

    // DO NOT RENAME!
    // Getter accessed from package via reflection. Must remain public
    [PublicAPI]
    public bool CsprojProcessedOnce
    {
      get => myCsprojProcessedOnce;
      set
      {
        myCsprojProcessedOnce = value;
        Save(true);
      }
    }
  }
}
#endif