using UnityEditor;

[InitializeOnLoad]
public class MissingConstructor
{
}

[InitializeOnLoad]
public class WithStaticConstructor
{
    static WithStaticConstructor()
    {
    }
}
