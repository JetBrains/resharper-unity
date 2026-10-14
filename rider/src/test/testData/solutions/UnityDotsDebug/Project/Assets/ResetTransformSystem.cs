using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ResetTransformSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var resetTransformJobEntity = new ResetTransformJobEntity();
        resetTransformJobEntity.Schedule();
    }
}
#if !UNITY_6000_4_OR_NEWER
public partial class ResetTransformSystemBase : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref LocalTransform transform, in RotationSpeed speed) =>
        {
            transform = new LocalTransform(){Position = float3.zero, Rotation = quaternion.identity}; //put breakpoint on this line
        }).ScheduleParallel();
    }
}
#endif
[BurstCompile]
partial struct ResetTransformJobEntity : IJobEntity
{
    void Execute(ref LocalTransform transform, in RotationSpeed speed)
    {
        transform = new LocalTransform(){Position = float3.zero, Rotation = quaternion.identity}; //put breakpoint on this line
    }
}
#if UNITY_6000_4_OR_NEWER
public partial class ResetTransformSystemBase : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            transform.ValueRW = new LocalTransform(){Position = float3.zero, Rotation = quaternion.identity}; //put breakpoint on this line
        }
    }
}
#endif
