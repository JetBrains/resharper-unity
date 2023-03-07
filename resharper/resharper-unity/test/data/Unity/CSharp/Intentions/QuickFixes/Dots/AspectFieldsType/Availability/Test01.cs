using Unity.Entities;


public struct SomeComponentData : IComponentData
{
}

public struct AnotherComponentData : IComponentData, IEnableableComponent
{
}

public readonly partial struct MyAspect : IAspect
{
    public readonly SomeComponentData SomeComponentData; //must be an error
    public readonly AnotherComponentData AnotherComponentData; //must be an error
}
