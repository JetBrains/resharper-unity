using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.FindUsages.Window
{
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  internal class FindUsagesWindow : EditorWindow
  {
    // [SerializeField] preserves state across layout/dock changes; SessionState is the domain-reload fallback.
    [SerializeField] private FindUsagesWindowTreeState myTreeViewState;
    [SerializeField] private bool IsDirty;
    [SerializeField] private string Target;
    [NonSerialized] private FindUsagesTreeView myTreeView;
    [NonSerialized] private FindUsagesSessionResult mySavedResult;

    public static void ShowResults(FindUsagesSessionResult result)
    {
      var window = GetWindow();
      window.SetDataToEditor(result);
    }

    [MenuItem("Window/Rider/Usages")]
    public static FindUsagesWindow GetWindow()
    {
      var window = GetWindow<FindUsagesWindow>();
      window.titleContent = new GUIContent("Usages Window");
      return window;
    }

    private void SetDataToEditor(FindUsagesSessionResult result)
    {
      mySavedResult = result;
      IsDirty = false;
      Target = result.Target;
      myTreeViewState = new FindUsagesWindowTreeState(result.Elements);
      myTreeView = new FindUsagesTreeView(myTreeViewState);
      myTreeView.Reload();
      myTreeView.ExpandAll();
      myTreeView.Repaint();
    }

    void OnEnable()
    {
      if (myTreeViewState != null)
      {
        myTreeView = new FindUsagesTreeView(myTreeViewState);
        return;
      }

      if (FindUsagesSessionState.TryLoad(out var result))
      {
        SetDataToEditor(result);
        IsDirty = true; // restored snapshot may be stale after domain reload
        return;
      }

      myTreeViewState = new FindUsagesWindowTreeState();
      myTreeView = new FindUsagesTreeView(myTreeViewState);
    }

    // Called once per domain from PluginEntryPoint, not from OnEnable/OnDisable (unreliable here).
    internal static void SaveOpenWindowStateBeforeReload()
    {
      foreach (var window in Resources.FindObjectsOfTypeAll<FindUsagesWindow>())
        window.SaveStateBeforeReload();
    }

    private void SaveStateBeforeReload()
    {
      if (mySavedResult == null)
        return;

      FindUsagesSessionState.Save(mySavedResult);
      Close();
    }

    internal static void RestoreAfterDomainReload()
    {
      if (FindUsagesSessionState.HasSavedState())
        GetWindow();
    }

    public void OnInspectorUpdate()
    {
      var count = SceneManager.sceneCount;
      for (int i = 0; i < count; i++)
      {
        if (SceneManager.GetSceneAt(i).isDirty)
          IsDirty = true;
      }

      Repaint();
    }

    void OnGUI()
    {
      var currentY = 0f;
      if (IsDirty) // the data can be out-of-date, notify user to update it from Rider
      {
        var text = "Save the scene and run Find Usages in Rider to get up-to-date results";
        var helpBox = GUILayoutUtility.GetRect(new GUIContent(text), EditorStyles.helpBox, GUILayout.MinHeight(40));
        currentY += helpBox.height;
        EditorGUI.HelpBox(helpBox, text, MessageType.Warning);
      }

      if (Target != null)
      {
        var text = $"Usages of '{Target}'";
        var textHeight = GUILayoutUtility.GetRect(new GUIContent(text), EditorStyles.label).height * 1.2f;
        GUI.Box(new Rect(0, currentY, position.width, textHeight), text);
        currentY += textHeight;
      }

      myTreeView?.OnGUI(new Rect(0, currentY, position.width, position.height - currentY));
    }
  }
}