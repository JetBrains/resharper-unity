using System;
using System.Collections.Generic;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.FindUsages;
using Xunit;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  public class FindUsagesSessionStateTest : IDisposable
  {
    private const string StateKey = "JetBrains.Rider.FindUsages.v1.State";

    private readonly FakeStringStore myStore = new FakeStringStore();
    private readonly IStringStore myOriginalStore;

    public FindUsagesSessionStateTest()
    {
      myOriginalStore = FindUsagesSessionState.Store;
      FindUsagesSessionState.Store = myStore;
    }

    public void Dispose()
    {
      FindUsagesSessionState.Store = myOriginalStore;
    }

    [Fact]
    public void HasSavedState_false_when_key_absent()
    {
      Assert.False(FindUsagesSessionState.HasSavedState());
    }

    [Fact]
    public void HasSavedState_true_when_key_present_and_does_not_consume()
    {
      myStore.Map[StateKey] = "non-empty";

      Assert.True(FindUsagesSessionState.HasSavedState());
      Assert.True(FindUsagesSessionState.HasSavedState());
      Assert.True(myStore.Map.ContainsKey(StateKey));
    }

    [Fact]
    public void TryLoad_false_and_no_erase_when_key_absent()
    {
      var loaded = FindUsagesSessionState.TryLoad(out var result);

      Assert.False(loaded);
      Assert.Null(result);
      Assert.False(myStore.EraseCalled);
    }

    [Fact]
    public void TryLoad_false_and_erases_when_base64_invalid()
    {
      myStore.Map[StateKey] = "not-base64-!!";

      var loaded = FindUsagesSessionState.TryLoad(out var result);

      Assert.False(loaded);
      Assert.Null(result);
      Assert.False(myStore.Map.ContainsKey(StateKey));
    }

    [Fact]
    public void TryLoad_false_and_erases_when_payload_corrupt()
    {
      // Valid base64, but bytes are not a valid Rd payload for FindUsagesSessionResult.
      myStore.Map[StateKey] = Convert.ToBase64String(new byte[] { 0xFF, 0xFE, 0xFD });

      var loaded = FindUsagesSessionState.TryLoad(out var result);

      Assert.False(loaded);
      Assert.Null(result);
      Assert.False(myStore.Map.ContainsKey(StateKey));
    }

    // RIDER-91427: the original bug lost AnimatorFindUsagesResult.Type/PathElements because
    // Unity's JsonUtility could not serialize the enum/array fields from this assembly. These
    // tests pin that the Rd codec preserves them across a full Save -> TryLoad cycle.
    [Fact]
    public void Save_then_TryLoad_preserves_AnimatorFindUsagesResult_fields()
    {
      var animator = new AnimatorFindUsagesResult(
        pathElements: new[] { "Root", "Layer", "State" },
        type: AnimatorUsageType.StateMachine,
        expandInTreeView: true,
        filePath: "Assets/Foo.controller",
        fileName: "Foo.controller",
        extension: ".controller");
      var original = new FindUsagesSessionResult("TargetMethod", new AssetFindUsagesResultBase[] { animator });

      FindUsagesSessionState.Save(original);
      var loaded = FindUsagesSessionState.TryLoad(out var restored);

      Assert.True(loaded);
      Assert.Equal("TargetMethod", restored.Target);
      var element = Assert.IsType<AnimatorFindUsagesResult>(Assert.Single(restored.Elements));
      Assert.Equal(AnimatorUsageType.StateMachine, element.Type);
      Assert.Equal(new[] { "Root", "Layer", "State" }, element.PathElements);
      Assert.Equal("Assets/Foo.controller", element.FilePath);
    }

    [Fact]
    public void Save_then_TryLoad_preserves_all_element_types_and_order()
    {
      var elements = new AssetFindUsagesResultBase[]
      {
        new HierarchyFindUsagesResult(
          pathElements: new[] { "Scene", "Parent", "Child" },
          rootIndices: new[] { 0, 1, 2 },
          expandInTreeView: false,
          filePath: "Assets/Scenes/Main.unity", fileName: "Main.unity", extension: ".unity"),
        new AnimatorFindUsagesResult(
          pathElements: new[] { "Root", "Layer", "State" },
          type: AnimatorUsageType.StateMachine,
          expandInTreeView: true,
          filePath: "Assets/Foo.controller", fileName: "Foo.controller", extension: ".controller"),
        new AnimationFindUsagesResult(
          expandInTreeView: false,
          filePath: "Assets/Clip.anim", fileName: "Clip.anim", extension: ".anim"),
        new AssetFindUsagesResult(
          expandInTreeView: false,
          filePath: "Assets/Data.asset", fileName: "Data.asset", extension: ".asset"),
      };
      var original = new FindUsagesSessionResult("MixedTarget", elements);

      FindUsagesSessionState.Save(original);
      var loaded = FindUsagesSessionState.TryLoad(out var restored);

      Assert.True(loaded);
      Assert.Equal("MixedTarget", restored.Target);
      Assert.Equal(4, restored.Elements.Length);

      var hierarchy = Assert.IsType<HierarchyFindUsagesResult>(restored.Elements[0]);
      Assert.Equal(new[] { "Scene", "Parent", "Child" }, hierarchy.PathElements);
      Assert.Equal(new[] { 0, 1, 2 }, hierarchy.RootIndices);

      var animator = Assert.IsType<AnimatorFindUsagesResult>(restored.Elements[1]);
      Assert.Equal(AnimatorUsageType.StateMachine, animator.Type);
      Assert.Equal(new[] { "Root", "Layer", "State" }, animator.PathElements);

      Assert.IsType<AnimationFindUsagesResult>(restored.Elements[2]);
      Assert.IsType<AssetFindUsagesResult>(restored.Elements[3]);
    }

    private sealed class FakeStringStore : IStringStore
    {
      public readonly Dictionary<string, string> Map = new Dictionary<string, string>();
      public bool EraseCalled { get; private set; }

      public string GetString(string key, string defaultValue) =>
        Map.TryGetValue(key, out var v) ? v : defaultValue;

      public void SetString(string key, string value) => Map[key] = value;

      public void EraseString(string key)
      {
        EraseCalled = true;
        Map.Remove(key);
      }
    }
  }
}
