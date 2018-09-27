using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
  // no need to set cache, because otherwise new Unity process will restore the value from the file cache. 
  //[Location("JetBrainsRiderPluginCache.txt", LocationAttribute.Location.LibraryFolder)] 
  internal class RiderScriptableSingleton: ScriptObjectSingleton<RiderScriptableSingleton>
  {
    [SerializeField] 
    private bool myCsprojProcessedOnce;
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