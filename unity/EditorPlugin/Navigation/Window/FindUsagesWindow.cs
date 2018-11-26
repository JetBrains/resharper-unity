using System;
using JetBrains.DataFlow;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagesWindow : EditorWindow
  {
    [SerializeField]
    public FindUsagesWindowTreeState myTreeViewState;

    [SerializeField]
    public bool IsDirty = false;
    
    [NonSerialized]
    private FindUsagesTreeView myTreeView;

    [MenuItem("Rider/Windows/Find usages")]
    public static void ShowWindow()
    {
      var window = GetWindow<FindUsagesWindow>();
      window.titleContent = new GUIContent ("Find usages");
      window.Show();
    }


    public void SetDataToEditor(RdFindUsageRequest[] data)
    {
      IsDirty = false;
      myTreeViewState = new FindUsagesWindowTreeState(data);
      myTreeView = new FindUsagesTreeView(myTreeViewState);
      myTreeView.Reload();
      myTreeView.Repaint();
    }
    
    void OnEnable ()
    {
      if (myTreeViewState == null)
      {
        myTreeViewState = new FindUsagesWindowTreeState();
      }

      myTreeView = new FindUsagesTreeView(myTreeViewState);
    }
    
    public void OnInspectorUpdate()
    {
      Repaint();
    }

    void OnGUI()
    {
      var count = SceneManager.sceneCount;
      for (int i = 0; i < count; i++)
      {
        if (SceneManager.GetSceneAt(i).isDirty) 
          IsDirty = true; 
      } 
      if (IsDirty) // the data can be out-of-date, notify user to update it from Rider
      {
        var text = "Save scene and ask Rider to find usages again to get up-to-date results.";
        var helpBox = GUILayoutUtility.GetRect(new GUIContent(text), EditorStyles.helpBox, GUILayout.MinHeight(40));
        EditorGUI.HelpBox(helpBox, text, MessageType.Warning);
        myTreeView?.OnGUI(new Rect(0, helpBox.height + 3, position.width, position.height - 3 - helpBox.height));
      }
      else
      {
        myTreeView?.OnGUI(new Rect(0, 0, position.width, position.height));
      }
    }
  }
}