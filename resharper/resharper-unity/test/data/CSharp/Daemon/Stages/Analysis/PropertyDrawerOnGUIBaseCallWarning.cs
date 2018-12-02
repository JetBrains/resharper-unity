using UnityEditor;
using UnityEngine;

public class Test : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var a = new Test();
        a.OnGUI(position, property, label);
        base.OnGUI(position, property, label);
        
        OnGUI();
        OnGUI(position, property, label);
    }
    
    public void OnGUI()
    {
        var a = new SerializedObject(new Object());
        var b = a.FindProperty("test");
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