using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
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
      var root = new FindUsagePathElement(0) {id = 0, depth = -1, displayName = "Usages result:"};

      var sceneNode = CreateSceneSubTree();
      root.AddChild(sceneNode);

      var prefabNode = CreatePrefabSubTree();
      root.AddChild(prefabNode);
      
      var scriptableObjectNode = CreateScriptableObjectSubTree();
      root.AddChild(scriptableObjectNode);
      
      SetupDepthsFromParentsAndChildren(root);
      return root;
    }

    private TreeViewItem CreateSceneSubTree()
    {
      var scenes = new FindUsagePathElement(0) {id = 1, displayName = "Scenes"};
      CreateSubTree(scenes, myState.SceneElements.ToArray(), 4);
      return scenes;
    }
    
    private TreeViewItem CreatePrefabSubTree()
    {
      var prefabs = new FindUsagePathElement(1) {id = 2, displayName = "Prefabs"};
      CreateSubTree(prefabs, myState.PrefabElements.ToArray(), 1_000_000_000);
      return prefabs;
    }
    
    private TreeViewItem CreateScriptableObjectSubTree()
    {
      var scriptableObject = new FindUsagePathElement(2) {id = 3, displayName = "Scriptable Objects"};

      var startId = 2_000_000_000;
      foreach (var usageElement in myState.ScriptableObjectElements.ToArray())
      {
        var id = startId++;
        findResultItems[id] = new FindUsagesTreeViewItem(-1, usageElement)
        {
          id = id,
          displayName = usageElement.FilePath,
          icon = (Texture2D) EditorGUIUtility.IconContent(usageElement.TerminalNodeImage).image
        };
        scriptableObject.AddChild(findResultItems[id]); 
      }

      return scriptableObject;
    }
    

    private void CreateSubTree(FindUsagePathElement element, IEnumerable<AbstractUsageElement> data, int startId)
    {
      var fileNames = new Dictionary<string, FindUsagePathElement>();
      var curFileNameId = 0;
      foreach (var usageElement in data)
      {
        FindUsagePathElement current = element;

        var filePath = usageElement.FilePath;
        var dataFileName = filePath.Split('/').Last();
        if (!fileNames.ContainsKey(filePath))
        {
          current = current.CreateChild(new FindUsagePathElement(curFileNameId++)
          {
            id = startId++, displayName = dataFileName, 
            icon = (Texture2D)EditorGUIUtility.IconContent(usageElement.StartNodeImage).image
          }); 
          fileNames.Add(filePath, current);
        }
        else
        {
          current = fileNames[filePath];
        }

        var pathLength = usageElement.Path.Length;
        for (int i = 0; i < pathLength; i++)
        {
          var name = usageElement.Path[i];
          if (i + 1 == pathLength)
          {
            var id = startId++;
            findResultItems[id] = new FindUsagesTreeViewItem(usageElement.RootIndices[i], usageElement)
            {
              id = id,
              displayName = name,
              icon = (Texture2D) EditorGUIUtility.IconContent(usageElement.TerminalNodeImage).image
            };
            current.AddChild(findResultItems[id]); 
          }
          else
          {
            var rootIndex = usageElement.RootIndices[i];
            if (!current.HasChild(rootIndex))
            {
              current = current.CreateChild(new FindUsagePathElement(rootIndex)
              {
                id = startId++, displayName = name,
                icon = (Texture2D)EditorGUIUtility.IconContent(usageElement.NodeImage).image
              });
            }
            else
            {
              current = current.GetChild(rootIndex);
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
        ShowUtil.ShowUsageOnScene(sceneElement.FilePath, sceneElement.FileName, sceneElement.Path, sceneElement.RootIndices);
      
      if (request is PrefabElement || request is ScriptableObjectElement)
        ShowUtil.ShowFileUsage(request.FilePath);
    }
  }
}