using UnityEngine;

// Note that GameObject.AddComponent(string) is obsolete, and flagged as an error in Unity 2018.2

public class Test
{
    public void Method(GameObject go)
    {
        go.AddComponent("");
        go.AddComponent("UnknownType");
        go.AddComponent("INotAClass");
        go.AddComponent("   MonoBehaviourInGlobalNamespace");
        go.AddComponent("MonoBehaviourInGlobalNamespace   ");
        go.AddComponent("InvalidNamespace.UnknownType");
        go.AddComponent("Other.InvalidNamespace.UnknownType");
        go.AddComponent("WrongBaseType");
        go.AddComponent("WrongComponentBaseType");
        go.AddComponent("MonoBehaviourWithTypeParameter");
        go.AddComponent("MonoBehaviourInGlobalNamespace");
        go.AddComponent("MonoBehaviourInOtherNamespace");
        go.AddComponent("MonoBehaviourInNestedNamespace");
        go.AddComponent("Other.MonoBehaviourInOtherNamespace");
        go.AddComponent("Nested1.Nested2.Nested3.Nested4.MonoBehaviourInNestedNamespace");
        go.AddComponent("Other.MonoBehaviourInOtherNamespace.InvalidTrailingType");

        go.AddComponent("CrashReporting");  // Multiple candidates
        go.AddComponent("Grid");            // Multiple candidates
        go.AddComponent("Caching");
        go.AddComponent("UnityEngine.Caching");
        go.AddComponent("HingeJoint");

        // TODO: What about trailing dots?
        go.AddComponent("Other.MonoBehaviourInOtherNamesapce.");
    }
}

public class WrongBaseType
{
}

public class WrongComponentBaseType : Component
{
}

public interface INotAClass
{
}

public class MonoBehaviourInGlobalNamespace : MonoBehaviour
{
}

// This isn't a legal construct in Unity
public class MonoBehaviourWithTypeParameter<T> : MonoBehaviour
{
}

namespace Other
{
    public class MonoBehaviourInOtherNamespace : MonoBehaviour
    {
    }
}

namespace Nested1
{
    namespace Nested2
    {
        namespace Nested3
        {
            namespace Nested4
            {
                public class MonoBehaviourInNestedNamespace : MonoBehaviour
                {
                }
            }
        }
    }
}

public class Grid : MonoBehaviour
{
}

namespace Other
{
    public class Grid : MonoBehaviour
    {
    }
}
