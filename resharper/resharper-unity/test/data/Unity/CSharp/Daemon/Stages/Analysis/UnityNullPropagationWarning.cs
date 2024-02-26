using UnityEngine;
using JetBrains.Annotations;

public class Test : MonoBehaviour
{
    [NotDestroyed] private GameObject _notDestroyedField;
    [NotDestroyed] public Transform NotDestroyedProperty;
    
    public void Method(GameObject go, Component c, Transform t, Object o)
    {
        var name = o?.name;
        name = go?.name;
        name = c?.name;
        name = t?.name;
        name = gameObject?.name;
        name = this.gameObject?.name;
        name = transform?.name;
        name = this.transform?.name;
        name = GameObject.Find("Something")?.name;
    }

    public void NotDestroyedProcessing([NotDestroyed] Transform t)
    {
        name = t?.name; // t arg marked as NonDestroyed
        name = gameObject.Opt()?.name; // Opt return value marked as NotDestroyed
        name = GameObject.Find("Something").Opt()?.name; // Opt return value marked as NotDestroyed
        name = _notDestroyedField?.name; // _notDestroyedField marked as NotDestroyed
        name = NotDestroyedProperty?.name; // NotDestroyedProperty marked as NotDestroyed 
        var tmp = gameObject.Opt();
        name = tmp?.name; // Initial value returned by right expression marked as NotDestroyed
        if (gameObject.Opt() is var tmp2)
            name = tmp2?.name; // Initial value returned by matched expression marked as NotDestroyed
    }
}

public static class UnityObjectExtensions
{
    [return: JetBrains.Annotations.NotDestroyed]
    public static Object Opt(this Object obj) => obj ? obj : null;
}

namespace JetBrains.Annotations
{
    public class NotDestroyedAttribute : System.Attribute { }
}
