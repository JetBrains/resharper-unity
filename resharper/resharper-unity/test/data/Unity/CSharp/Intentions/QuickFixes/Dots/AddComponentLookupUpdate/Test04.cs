using Unity.Entities;

struct Bar : IComponentData, IEnableableComponent
{
}

partial struct Foo : ISystem
{
    private ComponentLookup<Bar> NotUpdatedComponentLookup1; //Must be marked with warning
}

partial struct Foo : ISystem
{
    private ComponentLookup<Bar> NotUpdatedComponent{caret}Lookup2; //Must be marked with warning
}