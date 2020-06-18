using UnityEngine;

public class Whatever : MonoBehaviour
{
}

public class SomethingElse : MonoBehaviour
{
}

public class ScriptableThing : ScriptableObject
{
}

public class Test01
{
    public void Method(GameObject o)
    {
        o.GetComponent("Whateve{caret}r");
        o.GetComponent("SomethingElse");
    }

    public void Method2(GameObject o)
    {
        o.AddComponent("SomethingElse");
        ScriptableObject.CreateInstance("ScriptableThing");
    }
}

public class Test02
{
    public void Method(GameObject o)
    {
        o.GetComponent("Whatever");
        o.GetComponent("PlayableDirector");
    }
}
