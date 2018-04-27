using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
  [Location("JetBrainsRiderPluginCache.txt", LocationAttribute.Location.LibraryFolder)]
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