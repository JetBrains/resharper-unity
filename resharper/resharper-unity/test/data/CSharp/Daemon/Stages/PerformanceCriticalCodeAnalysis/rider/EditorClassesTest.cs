public class TestEditor : Editor
{
    public void DrawHeader() { }
    public void OnInspectorGUI() { }
    public void OnInteractivePreviewGUI() { }
    public void OnPreviewGUI() { }
    public void OnSceneGUI() { }
}

public class TestEditorWindow : EditorWindow
{
    public void OnGUI() { }
    public void OnInspectorUpdate() { }
}

public class TestPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { }
}