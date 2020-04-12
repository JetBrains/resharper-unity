using UnityEngine;

public class Test
{
    public void Method()
    {
        var s1 = ScriptableObject.CreateInstance("");
        s1 = ScriptableObject.CreateInstance("UnknownType");
        s1 = ScriptableObject.CreateInstance("INotAClass");
        s1 = ScriptableObject.CreateInstance("    ScriptableObjectInGlobalNamespace");
        s1 = ScriptableObject.CreateInstance("ScriptableObjectInGlobalNamespace   ");
        s1 = ScriptableObject.CreateInstance("InvalidNamespace.UnknownType");
        s1 = ScriptableObject.CreateInstance("Other.InvalidNamespace.UnknownType");
        s1 = ScriptableObject.CreateInstance("WrongBaseType");
        s1 = ScriptableObject.CreateInstance("ScriptableObjectWithTypeParameter");
        s1 = ScriptableObject.CreateInstance("ScriptableObjectInGlobalNamespace");
        s1 = ScriptableObject.CreateInstance("ScriptableObjectInOtherNamespace");
        s1 = ScriptableObject.CreateInstance("ScriptableObjectInNestedNamespace");
        s1 = ScriptableObject.CreateInstance("Other.ScriptableObjectInOtherNamespace");
        s1 = ScriptableObject.CreateInstance("Nested1.Nested2.Nested3.Nested4.ScriptableObjectInNestedNamespace");
        s1 = ScriptableObject.CreateInstance("Other.ScriptableObjectInOtherNamespace.InvalidTrailingType");

        s1 = ScriptableObject.CreateInstance("MultipleCandidates");

        // TODO: What about trailing dots?
        s1 = ScriptableObject.CreateInstance("Other.ScriptableObjectInOtherNamespace.");
    }
}

public class WrongBaseType
{
}

public interface INotAClass
{
}

public class ScriptableObjectInGlobalNamespace : ScriptableObject
{
}

public class MultipleCandidates : ScriptableObject
{
}

// This isn't a legal construct in Unity
public class ScriptableObjectWithTypeParameter<T> : ScriptableObject
{
}

namespace Other
{
    public class ScriptableObjectInOtherNamespace : ScriptableObject
    {
    }

    public class MultipleCandidates : ScriptableObject
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
                public class ScriptableObjectInNestedNamespace : ScriptableObject
                {
                }
            }
        }
    }
}
