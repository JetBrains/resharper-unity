using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagesTreeView : TreeView
  {
    
    private Dictionary<int, FindUsagesTreeViewItem> findResultItems = new Dictionary<int, FindUsagesTreeViewItem>();
    
    private readonly FindUsagesWindowTreeState myState;

    public FindUsagesTreeView(FindUsagesWindowTreeState state) : base(state)
    {
      myState = state;
      Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
      var root = new FindUsagePathElement {id = 0, depth = -1, displayName = "Usages result:"};
      var scenes = new FindUsagePathElement() {id = 1, displayName = "Scenes"};
      var prefabs = new FindUsagePathElement() {id = 2, displayName = "Prefabs"};

      int sceneId = 3;
      //int prefabId = 1_000_000_000;
 
      foreach (var sceneElement in myState.SceneElements)
      {
        FindUsagePathElement current = scenes;

        var sceneName = sceneElement.SceneName;
        if (!current.HasChild(sceneName))
        {
          current = current.CreateChild(new FindUsagePathElement
          {
            id = sceneId++, displayName = sceneElement.SceneName, 
            icon = (Texture2D)EditorGUIUtility.IconContent("UnityLogo").image
          }); 
        }
        else
        {
          current = current.GetChild(sceneName);
        }

        var pathLength = sceneElement.Path.Length;
        for (int i = 0; i < pathLength; i++)
        {
          var name = sceneElement.Path[i];
          if (i + 1 == pathLength)
          {
            var id = sceneId++;
            findResultItems[id] = new FindUsagesTreeViewItem(sceneElement)
            {
              id = id,
              displayName = name,
              icon = (Texture2D) EditorGUIUtility.IconContent("GameObject Icon").image
            };
            current.AddChild(findResultItems[id]); 
            
          }
          else
          {
            if (!current.HasChild(name))
            {
              current = current.CreateChild(new FindUsagePathElement
              {
                id = sceneId++, displayName = name,
                icon = (Texture2D)EditorGUIUtility.IconContent("GameObject Icon").image
              });
            }
            else
            {
              current = current.GetChild(name);
            }
          }
        }
      }

      
      root.AddChild(scenes);
      root.AddChild(prefabs);
      SetupDepthsFromParentsAndChildren (root);
      return root;
    }

    protected override void DoubleClickedItem(int id)
    {
       if (!findResultItems.ContainsKey(id))
         return;

      var request = findResultItems[id].SceneElement;
      EntryPoint.ShowUsageOnScene(request.SceneName, request.Path, request.LocalId);
    }
  }
}