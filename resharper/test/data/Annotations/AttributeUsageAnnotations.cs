using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

// Class only
[AddComponentMenu("Transform/Follow Transform")]
public class TestAddComponentMenu : MonoBehaviour
{
    [AddComponentMenu("Not allowed on fields")] public int Field;
    [AddComponentMenu("Not allowed on properties")] public int Property { get; set; }
    [AddComponentMenu("Not allowed on methods")] public void Method() { }
}

// Class only - must derive from MonoBehaviour
// Does not inherit
[ExecuteInEditMode]
public class TestExecuteInEditMode : MonoBehaviour
{
    [ExecuteInEditMode] public int Field;
    [ExecuteInEditMode] public int Property { get; set; }
    [ExecuteInEditMode] public void Method() { }
}

// Field only
[HideInInspector]
public class TestHideInInspector : MonoBehaviour
{
    [HideInInspector] public int Field;
    [HideInInspector] public string Property { get; set; }
    [HideInInspector] public void Whatever() { }
}

// Effectively undocumented. Appears to have the same usage as ImageEffectOpaque
// Method only - OnRenderImage, technically
[ImageEffectAfterScale]
public class TestImageEffectAfterScale : MonoBehaviour
{
    [ImageEffectAfterScale] public int Field;
    [ImageEffectAfterScale] public int Property { get; set; }
    [ImageEffectAfterScale] public void Method() { }
}

// Class only - must be derived from Component
// Allow inherit
[ImageEffectAllowedInSceneView]
public class TestImageEffectAllowedInSceneView : MonoBehaviour
{
    [ImageEffectAllowedInSceneView] public int Field;
    [ImageEffectAllowedInSceneView] public int Property { get; set; }
    [ImageEffectAllowedInSceneView] public void Method() { }
}

// Method only - OnRenderImage, technically
[ImageEffectOpaque]
public class TestImageEffectOpaque : MonoBehaviour
{
    [ImageEffectOpaque] public int Field;
    [ImageEffectOpaque] public int Property { get; set; }
    [ImageEffectOpaque] public void Method() { }
}

// Method only - OnRenderImage, technically
[ImageEffectTransformsToLDR]
public class TestImageEffectTransformsToLDR : MonoBehaviour
{
    [ImageEffectTransformsToLDR] public int Field;
    [ImageEffectTransformsToLDR] public int Property { get; set; }
    [ImageEffectTransformsToLDR] public void Method() { }
}

// Field only
[SerializeField]
public class TestSerializeField : MonoBehaviour
{
    [SerializeField] public int Field;
    [SerializeField] public int Property { get; set; }
    [SerializeField] public void Method() { }
}


// UnityEditor


// Method only (static void)
[DidReloadScripts]
public class TestDidReloadScripts : MonoBehaviour
{
    [DidReloadScripts] public int Field;
    [DidReloadScripts] public int Property { get; set; }
    [DidReloadScripts] public void Method() { }
}

// Method only
[OnOpenAsset]
public class TestOnOpenAsset : MonoBehaviour
{
    [OnOpenAsset] public int Field;
    [OnOpenAsset] public int Property { get; set; }
    [OnOpenAsset] public void Method() { }
}

// Method only
[PostProcessBuild]
public class TestPostProcessBuild : MonoBehaviour
{
    [PostProcessBuild] public int Field;
    [PostProcessBuild] public int Property { get; set; }
    [PostProcessBuild] public void Method() { }
}

// Method only
[PostProcessScene]
public class TestPostProcessScene : MonoBehaviour
{
    [PostProcessScene] public int Field;
    [PostProcessScene] public int Property { get; set; }
    [PostProcessScene] public void Method() { }
}

// Class only
[CanEditMultipleObjects]
public class TestCanEditMultipleObjects : MonoBehaviour
{
    [CanEditMultipleObjects] public int Field;
    [CanEditMultipleObjects] public int Property { get; set; }
    [CanEditMultipleObjects] public void Method() { }
}

// Class only
[CustomEditor(typeof(Animation))]
public class TestCustomEditor : MonoBehaviour
{
    [CustomEditor(typeof(Animation))] public int Field;
    [CustomEditor(typeof(Animation))] public int Property { get; set; }
    [CustomEditor(typeof(Animation))] public void Method() { }
}

// Method only - static
[DrawGizmo(GizmoType.Selected)]
public class TestCustomEditor : MonoBehaviour
{
    [DrawGizmo(GizmoType.Selected)] public int Field;
    [DrawGizmo(GizmoType.Selected)] public int Property { get; set; }
    [DrawGizmo(GizmoType.Selected)] public void Method() { }
}
