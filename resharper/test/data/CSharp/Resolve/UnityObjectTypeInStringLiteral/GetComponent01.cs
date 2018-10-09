using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        go.GetComponent("");
        go.GetComponent("UnknownType");
        go.GetComponent("INotAClass");
        go.GetComponent("   MonoBehaviourInGlobalNamespace");
        go.GetComponent("MonoBehaviourInGlobalNamespace   ");
        go.GetComponent("InvalidNamespace.UnknownType");
        go.GetComponent("Other.InvalidNamespace.UnknownType");
        go.GetComponent("WrongBaseType");
        go.GetComponent("WrongComponentBaseType");
        go.GetComponent("MonoBehaviourWithTypeParameter");
        go.GetComponent("MonoBehaviourInGlobalNamespace");
        go.GetComponent("MonoBehaviourInOtherNamespace");
        go.GetComponent("MonoBehaviourInNestedNamespace");
        go.GetComponent("Other.MonoBehaviourInOtherNamespace");
        go.GetComponent("Nested1.Nested2.Nested3.Nested4.MonoBehaviourInNestedNamespace");
        go.GetComponent("Other.MonoBehaviourInOtherNamespace.InvalidTrailingType");

        go.GetComponent("CrashReporting");  // Multiple candidates
        go.GetComponent("Grid");            // Multiple candidates
        go.GetComponent("Caching");
        go.GetComponent("UnityEngine.Caching");
        go.GetComponent("HingeJoint");

        // TODO: What about trailing dots?
        go.GetComponent("Other.MonoBehaviourInOtherNamesapce.");
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
