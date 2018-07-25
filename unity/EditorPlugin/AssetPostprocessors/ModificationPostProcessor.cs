using JetBrains.Rider.Unity.Editor.Utils;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class ModificationPostProcessor : UnityEditor.AssetModificationProcessor
  {
    private static void OnWillCreateAsset(string path)
    {
      var isCs = path.EndsWith(".cs.meta");
      if (isCs)
        RiderScriptableSingleton.Instance.HasModifiedScriptAssets = true;
    }

    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
      var isCs = assetPath.EndsWith(".cs.meta") || assetPath.EndsWith(".cs");

      if (isCs)
        RiderScriptableSingleton.Instance.HasModifiedScriptAssets = true;

      return AssetDeleteResult.DidNotDelete;
    }

    private static AssetMoveResult OnWillMoveAsset(string fromPath, string toPath)
    {
      var isCs = fromPath.EndsWith(".cs");

      if (isCs)
        RiderScriptableSingleton.Instance.HasModifiedScriptAssets = true;

      return AssetMoveResult.DidNotMove;
    }
  }
}