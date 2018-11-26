using System.Collections.Generic;
using System.Linq;
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

      var sceneNode = CreateSceneSubTree();
      root.AddChild(sceneNode);

      var prefabNode = CreatePrefabSubTree();
      root.AddChild(prefabNode);
      
      SetupDepthsFromParentsAndChildren (root);
      return root;
    }

    private TreeViewItem CreateSceneSubTree()
    {
      var scenes = new FindUsagePathElement() {id = 1, displayName = "Scenes"};
      CreateSubTree(scenes, myState.SceneElements.ToArray(), 3);
      return scenes;
    }
    
    private TreeViewItem CreatePrefabSubTree()
    {
      var prefabs = new FindUsagePathElement() {id = 2, displayName = "Prefabs"};
      CreateSubTree(prefabs, myState.PrefabElements.ToArray(), 1_000_000_000);
      return prefabs;
    }

    private void CreateSubTree(FindUsagePathElement element, IEnumerable<AbstractUsageElement> data, int startId)
    {
      foreach (var usageElement in data)
      {
        FindUsagePathElement current = element;

        var sceneName = usageElement.FilePath.Split('/').Last();
        if (!current.HasChild(sceneName))
        {
          current = current.CreateChild(new FindUsagePathElement
          {
            id = startId++, displayName = sceneName, 
            icon = (Texture2D)EditorGUIUtility.IconContent(usageElement.StartNodeImage).image
          }); 
        }
        else
        {
          current = current.GetChild(sceneName);
        }

        var pathLength = usageElement.Path.Length;
        for (int i = 0; i < pathLength; i++)
        {
          var name = usageElement.Path[i];
          if (i + 1 == pathLength)
          {
            var id = startId++;
            findResultItems[id] = new FindUsagesTreeViewItem(usageElement)
            {
              id = id,
              displayName = name,
              icon = (Texture2D) EditorGUIUtility.IconContent(usageElement.TerminalNodeImage).image
            };
            current.AddChild(findResultItems[id]); 
          }
          else
          {
            if (!current.HasChild(name))
            {
              current = current.CreateChild(new FindUsagePathElement
              {
                id = startId++, displayName = name,
                icon = (Texture2D)EditorGUIUtility.IconContent(usageElement.NodeImage).image
              });
            }
            else
            {
              current = current.GetChild(name);
            }
          }
        }
      }
    }
    
    protected override void DoubleClickedItem(int id)
    {
      if (!findResultItems.ContainsKey(id))
      {
        SetExpanded(id, true);
        return;
      }

      var request = findResultItems[id].UsageElement;
      if (request is SceneElement sceneElement)
        ShowUtil.ShowUsageOnScene(sceneElement.FilePath, sceneElement.Path, sceneElement.RootIndices);
      
      if (request is PrefabElement prefabElement)
        ShowUtil.ShowPrefabUsage(prefabElement.FilePath, prefabElement.Path);
    }
  }
}