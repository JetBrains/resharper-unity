// ${RUN:Wrap with 'EnabledRefRW'}
using Unity.Entities;

public struct SomeComponentData : IComponentData, IEnableableComponent
{
}

public readonly partial struct MyAspect : IAspect
{
    public readonly Some{caret}ComponentData SomeComponentData; //must be an error
}