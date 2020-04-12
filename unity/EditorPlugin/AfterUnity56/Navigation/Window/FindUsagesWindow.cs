using System;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
  internal class FindUsagesWindow : EditorWindow
  {
    [SerializeField]
    public FindUsagesWindowTreeState myTreeViewState;

    [SerializeField]
    public bool IsDirty = false;
    
    [SerializeField]
    public string Target = null;
    
    [NonSerialized]
    private FindUsagesTreeView myTreeView;

    public static FindUsagesWindow GetWindow(string target)
    {
      var window = GetWindow();
      window.Target = target;
      return window;
    }

    [MenuItem("Window/Rider/Usages")]
    public static FindUsagesWindow GetWindow()
    {
      var window = GetWindow<FindUsagesWindow>();
      window.titleContent = new GUIContent ("Usages Window");
      return window;
    }

    public void SetDataToEditor(AssetFindUsagesResultBase[] data)
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
        var textHeight= GUILayoutUtility.GetRect(new GUIContent(text), EditorStyles.label).height * 1.2f;
        GUI.Box(new Rect(0, currentY, position.width, textHeight),  text);
        currentY += textHeight;
      }

      myTreeView?.OnGUI(new Rect(0, currentY, position.width, position.height - currentY));
    }
  }
}