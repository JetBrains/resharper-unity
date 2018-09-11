using UnityEngine;
using UnityEditor;

public class TestDrawGizmoMethod
{
    [DrawGizmo]
    public void NotStatic(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    public static int WrongReturnType(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    public static void WrongReturnType(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    public static void OneCorrectParameter(GameObject arg1, GizmoType arg2) { }
	
    [DrawGizmo]
    public static void OneParameter(GizmoType arg2) { }
	
    [DrawGizmo]
    public static void WrongTypeParameters<T>(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    public static void JustRight(Component arg1, GizmoType arg2) { }

    [DrawGizmo]
    private static void JustRight2(MonoBehaviour arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight3(TestClass1 arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight4(TestClass2 arg1, GizmoType arg2) { }
    
    [DrawGizmo]
    private static void JustRight4(Transform arg1, GizmoType arg2) { }

    public class TestClass1 : MonoBehaviour
    {
    }
    
    public class TestClass2 : TestClass1
    {
    }
}

