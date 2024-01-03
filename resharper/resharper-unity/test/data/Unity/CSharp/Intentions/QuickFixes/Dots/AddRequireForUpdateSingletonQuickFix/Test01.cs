using Unity.Entities;

public class ParentClass
{
    struct Bar : IComponentData, IEnableableComponent
    {
    }
}

partial struct Foo : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var k = SystemAPI.GetSingleton{caret}<ParentClass.Bar>();//Must be marked with warning
    }

    public void OnDestroy(ref SystemState state)
    {
        
    }
}
