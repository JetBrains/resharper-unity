using Unity.Entities;

public partial struct RotationSystem : ISystem
{
    public void OnCreate(ref SystemState state){}
    public void OnDestroy(ref SystemState state){}

    public void OnUpdate(ref SystemState state)
    {

        foreach (var a in  SystemAPI.Query<RefRW<ComponentA>, ComponentB>())
        {
        }

        foreach (var a in SystemAPI.Query<ComponentA>())
        {
        }
    }
}

struct ComponentA : IComponentData
{
    public float WriteValue;
}


struct ComponentB : IComponentData
{
    public float ReadValue;
}
