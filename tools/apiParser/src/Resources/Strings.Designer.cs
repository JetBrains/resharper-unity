namespace ApiParser.Resources
{
  using System;
  using JetBrains.Application.I18n;
  using JetBrains.DataFlow;
  using JetBrains.Diagnostics;
  using JetBrains.Lifetimes;
  using JetBrains.Util;
  using JetBrains.Util.Logging;
  
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
  public static class Strings
  {
    private static readonly ILogger ourLog = Logger.GetLogger("ApiParser.Resources.Strings");

    static Strings()
    {
      CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, (lifetime, instance) =>
      {
        lifetime.Bracket(() =>
          {
            ourResourceManager = new Lazy<JetResourceManager>(
              () =>
              {
                return instance
                  .CreateResourceManager("ApiParser.Resources.Strings", typeof(Strings).Assembly);
              });
          },
          () =>
          {
            ourResourceManager = null;
          });
      });
    }
    
    private static Lazy<JetResourceManager> ourResourceManager = null;
    
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    public static JetResourceManager ResourceManager
    {
      get
      {
        var resourceManager = ourResourceManager;
        if (resourceManager == null)
        {
          return ErrorJetResourceManager.Instance;
        }
        return resourceManager.Value;
      }
    }

    public static string AssetPostprocessor_OnGeneratedCSProjectFiles_Description => ResourceManager.GetString("AssetPostprocessor_OnGeneratedCSProjectFiles_Description");
    public static string AssetPostprocessor_OnPreGeneratingCSProjectFiles_Description => ResourceManager.GetString("AssetPostprocessor_OnPreGeneratingCSProjectFiles_Description");
    public static string AssetPostprocessor_OnGeneratedCSProject_Description => ResourceManager.GetString("AssetPostprocessor_OnGeneratedCSProject_Description");
    public static string AssetPostprocessor_OnGeneratedSlnSolution_Description => ResourceManager.GetString("AssetPostprocessor_OnGeneratedSlnSolution_Description");
    public static string AssetPostprocessor_OnPostprocessAllAssets_Description => ResourceManager.GetString("AssetPostprocessor_OnPostprocessAllAssets_Description");
    public static string MonoBehaviour_OnRectTransformDimensionsChange_Description => ResourceManager.GetString("MonoBehaviour_OnRectTransformDimensionsChange_Description");
    public static string ScriptableObject_OnValidate_Description => ResourceManager.GetString("ScriptableObject_OnValidate_Description");
    public static string Editor_OnPreSceneGUI_Description => ResourceManager.GetString("Editor_OnPreSceneGUI_Description");
    public static string Editor_OnSceneDrag_Description => ResourceManager.GetString("Editor_OnSceneDrag_Description");
    public static string Editor_OnSceneDrag_sceneView_Description => ResourceManager.GetString("Editor_OnSceneDrag_sceneView_Description");
    public static string Editor_OnSceneDrag_index_Description => ResourceManager.GetString("Editor_OnSceneDrag_index_Description");
    public static string Editor_HasFrameBounds_Description => ResourceManager.GetString("Editor_HasFrameBounds_Description");
    public static string Editor_OnGetFrameBounds_Description => ResourceManager.GetString("Editor_OnGetFrameBounds_Description");
    public static string EditorWindow_ModifierKeysChanged_Description => ResourceManager.GetString("EditorWindow_ModifierKeysChanged_Description");
    public static string EditorWindow_ShowButton_Description => ResourceManager.GetString("EditorWindow_ShowButton_Description");
    public static string EditorWindow_OnBecameVisible_Description => ResourceManager.GetString("EditorWindow_OnBecameVisible_Description");
    public static string EditorWindow_OnBecameInvisible_Description => ResourceManager.GetString("EditorWindow_OnBecameInvisible_Description");
    public static string EditorWindow_OnDidOpenScene_Description => ResourceManager.GetString("EditorWindow_OnDidOpenScene_Description");
    public static string EditorWindow_OnAddedAsTab_Description => ResourceManager.GetString("EditorWindow_OnAddedAsTab_Description");
    public static string EditorWindow_OnBeforeRemovedAsTab_Description => ResourceManager.GetString("EditorWindow_OnBeforeRemovedAsTab_Description");
    public static string EditorWindow_OnTabDetached_Description => ResourceManager.GetString("EditorWindow_OnTabDetached_Description");
    public static string EditorWindow_OnMainWindowMove_Description => ResourceManager.GetString("EditorWindow_OnMainWindowMove_Description");
    public static string ScriptableObject_Reset_Description => ResourceManager.GetString("ScriptableObject_Reset_Description");
    public static string EditorWindow_ShowButton_rect_Description => ResourceManager.GetString("EditorWindow_ShowButton_rect_Description");
  }
}