using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// RIDER-102087. `SystemAPI.Query` is source-generator scaffolding - every method in the chain is declared as
// `throw InternalCompilerInterface.ThrowCodeGenException()` - so evaluating it in the debugger only works if it
// is lowered onto the real query API first. Two type arguments on purpose: that is what real DOTS code looks
// like, and a lowering that only handles a single type argument silently does nothing here.
public partial struct QueryTransformSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            transform.ValueRW = new LocalTransform { Position = float3.zero, Rotation = quaternion.identity }; //put breakpoint on this line
        }
    }
}
