using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.FindUsages.Window
{
  internal class FindUsagesTreeView : TreeView<int>
  {
    private readonly Dictionary<int, FindUsagesTreeViewItem> myFindResultItems = new();
    private readonly Dictionary<int, int> myAnimatorItemIdToPathElementsCount = new();

    private readonly FindUsagesWindowTreeState myState;

    public FindUsagesTreeView(FindUsagesWindowTreeState state) : base(state)
    {
      myState = state;
      Reload();
    }

    protected override TreeViewItem<int> BuildRoot()
    {
      var root = new FindUsagePathElement(0) {id = 0, depth = -1, displayName = "Usages result:"};

      var sceneNode = CreateSceneSubTree();
      root.AddChild(sceneNode);

      var prefabNode = CreatePrefabSubTree();
      root.AddChild(prefabNode);

      var scriptableObjectNode = CreateScriptableObjectSubTree();
      root.AddChild(scriptableObjectNode);

      var animatorSubTree = CreateAnimatorSubTree();
      root.AddChild(animatorSubTree);

      var animationSubTree = CreateAnimationEventsSubTree();
      root.AddChild(animationSubTree);

      SetupDepthsFromParentsAndChildren(root);
      return root;
    }

    private TreeViewItem<int> CreateSceneSubTree()
    {
      var scenes = new FindUsagePathElement(0) {id = 1, displayName = "Scenes"};
      CreateSubTree(scenes, myState.SceneElements.ToArray(), 50_000);
      return scenes;
    }

    private TreeViewItem<int> CreatePrefabSubTree()
    {
      var prefabs = new FindUsagePathElement(1) {id = 2, displayName = "Prefabs"};
      CreateSubTree(prefabs, myState.PrefabElements.ToArray(), 100_000);
      return prefabs;
    }

    private TreeViewItem<int> CreateScriptableObjectSubTree()
    {
      var scriptableObject = new FindUsagePathElement(2) {id = 3, displayName = "Scriptable Objects"};

      var startId = 150_000;
      foreach (var usageElement in myState.ScriptableObjectElements.ToArray())
      {
        var id = startId++;
        myFindResultItems[id] = new FindUsagesTreeViewItem(-1, usageElement)
        {
          id = id,
          displayName = usageElement.FilePath,
          icon = (Texture2D) EditorGUIUtility.IconContent(usageElement.TerminalNodeImage).image
        };
        scriptableObject.AddChild(myFindResultItems[id]);
      }

      return scriptableObject;
    }

    private TreeViewItem<int> CreateAnimatorSubTree()
    {
        var animator = new FindUsagePathElement(3) {id = 4, displayName = "Animator"};
        var startId = 200_000;
        foreach (var animatorElement in myState.AnimatorElements.ToArray())
        {
            CreateAnimatorSubTree(animator, animatorElement, ref startId);
        }
        return animator;
    }

    private void CreateAnimatorSubTree([NotNull] FindUsagePathElement findUsagePathElement,
                                       [NotNull] AnimatorElement animatorElement, ref int id)
    {
        var currentParent = findUsagePathElement;
        for (int i = 0, pathElementsLength = animatorElement.PathElements.Length; i < pathElementsLength; i++)
        {
            var icon = i == pathElementsLength - 1
                ? animatorElement.TerminalNodeImage
                : AnimatorElement.AnimatorStateMachineIcon;
            var findUsagesTreeViewItem = new FindUsagesTreeViewItem(id, animatorElement)
            {
                id = id,
                displayName = animatorElement.PathElements[i],
                icon = (Texture2D) EditorGUIUtility.IconContent(icon).image
            };
            myFindResultItems[id] = findUsagesTreeViewItem;
            myAnimatorItemIdToPathElementsCount[id] = i + 1;
            currentParent.AddChild(findUsagesTreeViewItem);
            currentParent = findUsagesTreeViewItem;
            id++;
        }
    }

    private TreeViewItem<int> CreateAnimationEventsSubTree()
    {
        var animationTreeRoot = new FindUsagePathElement(4) {id = 5, displayName = "Animations"};
        var startId = 250_000;
        foreach (var animationEventElement in myState.AnimationElements.ToArray())
        {
            CreateAnimationEventItem(animationTreeRoot, animationEventElement, ref startId);
        }
        return animationTreeRoot;
    }

    private void CreateAnimationEventItem([NotNull] TreeViewItem<int> animationTreeRoot,
                                          [NotNull] AbstractUsageElement animationElement,
                                          ref int id)
    {
        var findUsagesTreeViewItem = new FindUsagesTreeViewItem(id, animationElement)
        {
            id = id,
            displayName = animationElement.FileName,
            icon = (Texture2D) EditorGUIUtility.IconContent(animationElement.TerminalNodeImage)?.image
        };
        myFindResultItems[id] = findUsagesTreeViewItem;
        animationTreeRoot.AddChild(findUsagesTreeViewItem);
        id++;
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
            myFindResultItems[id] = new FindUsagesTreeViewItem(usageElement.RootIndices[i], usageElement)
            {
              id = id,
              displayName = name,
              icon = (Texture2D) EditorGUIUtility.IconContent(usageElement.TerminalNodeImage).image
            };
            current.AddChild(myFindResultItems[id]);
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
      if (!myFindResultItems.ContainsKey(id))
      {
        SetExpanded(id, true);
        return;
      }

      var request = myFindResultItems[id].UsageElement;
      switch (request)
      {
          case SceneElement sceneElement:
              ShowUtil.ShowUsageOnScene(sceneElement.FilePath, sceneElement.FileName, sceneElement.Path, sceneElement.RootIndices);
              break;
          case PrefabElement _:
          case ScriptableObjectElement _:
              ShowUtil.ShowFileUsage(request.FilePath);
              break;
          case AnimatorElement animatorElement:
              var elements = animatorElement.PathElements;
              var range = elements.ToList().GetRange(0, myAnimatorItemIdToPathElementsCount[id]);
              ShowUtil.ShowAnimatorUsage(range.ToArray(), animatorElement.FilePath);
              break;
          case AnimationElement animationElement:
              ShowUtil.ShowAnimationEventUsage(animationElement.FilePath);
              break;
      }
    }
  }
}