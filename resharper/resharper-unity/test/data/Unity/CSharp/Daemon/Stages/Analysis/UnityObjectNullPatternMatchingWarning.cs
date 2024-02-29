using UnityEngine;

public class PatternMatching : MonoBehaviour
{
    public int Method(GameObject go, Component c, Transform t, Object o)
    {
        var counter = 0;
        if (go is null) counter++;
        if (c is null) counter++;
        if (t is null) counter++;
        if (o is null) counter++;
        if (go is not null) counter++;
        if (c is not null) counter++;
        if (t is not null) counter++;
        if (o is not null) counter++;
        if (go is {}) counter++;
        if (c is {}) counter++;
        if (t is {}) counter++;
        if (o is {}) counter++;
        if (go is not {}) counter++;
        if (c is not {}) counter++;
        if (t is not {}) counter++;
        if (o is not {}) counter++;
        if (go is {name: ""}) counter++;
        if (c is {name: ""}) counter++;
        if (t is {name: ""}) counter++;
        if (o is {name: ""}) counter++;
        if (go is not {name: ""}) counter++;
        if (c is not {name: ""}) counter++;
        if (t is not {name: ""}) counter++;
        if (o is not {name: ""}) counter++;
        if (t is {parent: {}}) counter++;
        if (t is {parent: not {}}) counter++;
        if (t is RectTransform) counter++;
        if (t is not RectTransform) counter++;
        if (t is RectTransform {parent: {}}) counter++;
        if (t is RectTransform {parent: not null}) counter++;
        if (t is RectTransform {parent: null}) counter++;
        counter += t switch
        {
            RectTransform => 1,
            { } when t.parent is {} => 2,
            null => 2,
            _ => 3
        };
        switch (t)
        {
            case RectTransform:
                counter++;
                break;
            case {} when t.parent is {}:
                counter++;
                break;
            case null:
                counter++;
                break;
            default:
                counter++;
                break;
        }
        
        if (o.Opt() is not {name: ""}) counter++;
        if (go.Opt() is null) counter++;
        if (t.Opt() is RectTransform) counter++;
        if (t.Opt() is not RectTransform) counter++;
        if (t.Opt() is RectTransform {parent: {}}) counter++;
        if (t.Opt() is RectTransform {parent: var parent1} && parent1.Opt() is {}) counter++;
        if (t.Opt() is RectTransform {parent: var parent2} && parent2.Opt() is not null) counter++;
        if (t.Opt() is RectTransform {parent: var parent3} && parent3.Opt() is null) counter++;
        if (t.parent is var parent4 && parent4.Opt() is not null) counter++;
        counter += t.Opt() switch
        {
            RectTransform => 1,
            { parent: var parent5 } when parent5.Opt() is null => 2,
            { } when t.parent.Opt() is {} => 2,
            { } => 2,
            _ => 3
        };
        switch (t.Opt())
        {
            case RectTransform:
                counter++;
                break;
            case { parent: var parent6 } when parent6.Opt() is null => 2:
                break;
            case {} when t.parent.Opt() is {}:
                counter++;
                break;
            case null:
                counter++;
                break;
            default:
                counter++;
                break;
        }
        return counter;
    }
}

public static class UnityObjectExtensions
{
    [return: JetBrains.Annotations.NotDestroyed]
    public static T Opt<T>(this T obj) where T : Object => obj ? obj : null;
}

namespace JetBrains.Annotations
{
    public class NotDestroyedAttribute : System.Attribute { }
}
