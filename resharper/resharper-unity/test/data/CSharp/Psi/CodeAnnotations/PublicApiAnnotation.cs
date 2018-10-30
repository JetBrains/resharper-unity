using JetBrains.Annotations;

[PublicAPI]
public class MyUnusedClass
{
    public string UnusedProperty { get; set; }
    public void UnusedMethod() {}
}

public class MyUnannotatedClass
{
    public string UnusedProperty { get; set; }
    public void UnusedMethod() {}
}
