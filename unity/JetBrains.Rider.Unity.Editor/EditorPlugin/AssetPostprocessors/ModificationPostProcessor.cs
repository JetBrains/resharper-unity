using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class ModificationPostProcessor : UnityEditor.AssetModificationProcessor
  {
    public const string ModifiedSource = "com.jetbrains.rider.modifiedsourcefile";
    
    private static void OnWillCreateAsset(string path)
    {
      var isCs = path.EndsWith(".cs.meta");
      if (isCs)
        EditorPrefs.SetBool(ModifiedSource, true);
    }

    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
      var isCs = assetPath.EndsWith(".cs.meta") || assetPath.EndsWith(".cs");

      if (isCs)
        EditorPrefs.SetBool(ModifiedSource, true);

      return AssetDeleteResult.DidNotDelete;
    }

    private static AssetMoveResult OnWillMoveAsset(string fromPath, string toPath)
    {
      var isCs = fromPath.EndsWith(".cs");

      if (isCs)
        EditorPrefs.SetBool(ModifiedSource, true);

      return AssetMoveResult.DidNotMove;
    }
  }
}