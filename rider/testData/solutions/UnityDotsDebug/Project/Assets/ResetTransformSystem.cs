using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ResetTransformSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new ResetTransformJob();
        job.Schedule();
    }
}

[BurstCompile]
partial struct ResetTransformJob : IJobEntity
{
    void Execute(ref LocalTransform transform, in RotationSpeed speed)
    {
        transform = new LocalTransform(){Position = float3.zero, Rotation = quaternion.identity};
    }
}