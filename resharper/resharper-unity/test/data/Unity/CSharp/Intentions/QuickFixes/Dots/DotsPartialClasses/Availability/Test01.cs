using Unity.Entities;

struct EnemySpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
    }

    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}

public class BigmapSystemBase : SystemBase
{
    protected override void OnUpdate()
    {
    }
}


struct ShorelineSettingsAspect : IAspect
{
}


readonly struct BigmapSettingsAspect : IAspect
{
}

struct Factory4 : IComponentData //shouldn't be partial
{
}