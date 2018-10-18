using UnityEngine;
using UnityEditor;

public class TestDrawGizmoMethod
{
    [DrawGizmo]
    public void NotStatic(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    public static int WrongReturnType(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    public static void OneCorrectParameter(GameObject arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    public static void OneCorrectParameter1(Editor arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    public static void OneCorrectParameter2(MonoBehaviour arg1, string arg2) { }
    
    [DrawGizmo]
    public static void OneCorrectParameter3(Collider arg1, string arg2) { }
	
    [DrawGizmo]
    public static void OneParameter(GizmoType arg2) { }
	
    [DrawGizmo]
    public static void WrongTypeParameters<T>(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    private static void TooManyParams(string arg1, Transform arg2, GizmoType arg3) { }
    
    [DrawGizmo]
    private static void TooManyParams2(Transform arg1, GizmoType arg2, string arg3) { }
    
    [DrawGizmo]
    public static void JustRight(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    private static void JustRight2(MonoBehaviour arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight3(TestClass1 arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight4(TestClass2 arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight5(Transform arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight6(Collider arg1, GizmoType arg2) { }
    
    public class TestClass1 : MonoBehaviour
    {
    }
    
    public class TestClass2 : TestClass1
    {
    }
}

