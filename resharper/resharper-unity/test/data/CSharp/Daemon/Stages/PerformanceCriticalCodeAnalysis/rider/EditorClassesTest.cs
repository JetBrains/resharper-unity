using UnityEngine;
using UnityEditor;


public class TestEditor : Editor
{
    public void DrawHeader() { Debug.Log("test"); }
    public void OnInspectorGUI() { Debug.Log("test"); }
    public void OnInteractivePreviewGUI() { Debug.Log("test"); }
    public void OnPreviewGUI() { Debug.Log("test"); }
    public void OnSceneGUI() { Debug.Log("test"); }
}

public class TestEditorWindow : EditorWindow
{
    public void OnGUI() { Debug.Log("test"); }
    public void OnInspectorUpdate() { Debug.Log("test"); }
}

public class TestPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { Debug.Log("test"); }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { Debug.Log("test"); }
}