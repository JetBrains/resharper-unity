using UnityEditor;

public class Class1
{
    [MenuItem("MyMenu/Log Selected Transform Name")]
    static void LogSelectedTransformName()
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name", true)]
    static bool ValidateLogSelectedTransformName()
    {
        return Selection.activeTransform != null;
    }
}

public class Class2
{
    [MenuItem("MyMenu/Log Selected Transform Name")]
    void LogSelectedTransformName()
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name", true)]
    bool ValidateLogSelectedTransformName()
    {
        return Selection.activeTransform != null;
    }
}