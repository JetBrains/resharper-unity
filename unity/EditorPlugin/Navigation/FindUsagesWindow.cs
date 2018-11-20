using System;
using JetBrains.DataFlow;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagesWindow : EditorWindow
  {
    [SerializeField]
    FindUsagesWindowTreeState myTreeViewState;
    
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
      Debug.Log("New data arrived");
      myTreeViewState = new FindUsagesWindowTreeState(data);
      myTreeView = new FindUsagesTreeView(myTreeViewState);
      myTreeView.Reload();
      myTreeView.Repaint();
    }
    
    void OnEnable ()
    {
      if (myTreeViewState == null)
      {
        Debug.Log("Was recreated");
        myTreeViewState = new FindUsagesWindowTreeState();
      }

      myTreeView = new FindUsagesTreeView(myTreeViewState);
    }
    
    void OnGUI()
    {
      myTreeView.OnGUI(new Rect(0, 0, position.width, position.height));
    }
  }
}