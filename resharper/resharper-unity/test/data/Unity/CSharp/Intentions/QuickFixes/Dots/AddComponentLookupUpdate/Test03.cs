using Unity.Entities;

struct Bar : IComponentData, IEnableableComponent
{
}

partial struct Foo : ISystem
{
    private ComponentLookup<Bar> NotUpdatedComponent{caret}Lookup1; //Must be marked with warning
    private ComponentLookup<Bar> NotUpdatedComponentLookup2; //Must be marked with warning
    
    public void OnUpdate(ref SystemState state)
    {
    }
}
