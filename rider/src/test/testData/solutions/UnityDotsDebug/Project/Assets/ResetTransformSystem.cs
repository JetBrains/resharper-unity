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

[BurstCompile]
partial struct ResetTransformJobEntity : IJobEntity
{
    void Execute(ref LocalTransform transform, in RotationSpeed speed)
    {
        transform = new LocalTransform(){Position = float3.zero, Rotation = quaternion.identity}; //put breakpoint on this line
    }
}
