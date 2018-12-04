using UnityEditor;
using UnityEngine;

public class Test : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Make sure it only happens in calls to base
        base.OnGUI(position, property, label);
        
        // So these should not trigger
        var a = new Test();
        a.OnGUI(position, property, label);
        OnGUI();
        OnGUI(position, property, label);
    }
    
    public void OnGUI()
    {
        var a = new SerializedObject(new Object());
        var b = a.FindProperty("test");
        // We don't want to highlight this since we only want to catch accidental usages
        base.OnGUI(new Rect(), b, new GUIContent("test"));
    }
}

public class PlainClass
{
    public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        OnGUI(position, property, label);
    }
}