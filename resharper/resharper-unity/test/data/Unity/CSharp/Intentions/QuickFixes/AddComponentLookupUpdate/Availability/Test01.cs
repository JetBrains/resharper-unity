using Unity.Entities;

struct Bar : IComponentData, IEnableableComponent
{
}

partial struct Foo : ISystem
{
    private ComponentLookup<Bar> NotUpdatedComponentLookupFromAnotherClassDeclaration; //Must be marked with warning
    private ComponentLookup<Bar> UpdatedComponentLookupFromAnotherClassDeclaration;
}


partial struct Foo : ISystem
{
    private ComponentLookup<Bar> NotUpdatedComponentLookup; //Must be marked with warning
    private ComponentLookup<Bar> UpdatedByUpdateFunction;
    private ComponentLookup<Bar> UpdatedByInternalMethod;
    private ComponentLookup<Bar> UpdatedInStaticMethod;
    private ComponentLookup<Bar> UpdatedInOnCreateMethod; //Must be marked with warning - OnCreate method is skipped
    private ComponentLookup<Bar> UpdatedInOnDestroyMethod;//Must be marked with warning - OnDestroy method is skipped

    public void OnUpdate(ref SystemState state)
    {
        UpdatedByUpdateFunction.Update(ref state);
        UpdatedComponentLookupFromAnotherClassDeclaration.Update(ref state);
    }

    public void OnCreate(ref SystemState state)
    {
        UpdatedInOnCreateMethod.Update(ref state);
    }

    public void OnDestroy(ref SystemState state)
    {
        UpdatedInOnDestroyMethod.Update(ref state);
    }

    public static void StaticFunction(ref Foo sys, ref SystemState state)
    {
        sys.UpdatedInStaticMethod.Update(ref state);
    }

    private void customUpdate(ref SystemState state)
    {
        this.UpdatedByInternalMethod.Update(ref state);
    }
}
