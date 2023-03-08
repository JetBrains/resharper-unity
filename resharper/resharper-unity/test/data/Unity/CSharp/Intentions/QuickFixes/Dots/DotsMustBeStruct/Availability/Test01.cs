using Unity.Entities;

class EnemySpawnSystem : ISystem
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

class ShorelineSettingsAspect : IAspect
{
}

public class MyJobEntity : IJobEntity
{
}

class Factory4 : IComponentData //could be a class instance
{
}
