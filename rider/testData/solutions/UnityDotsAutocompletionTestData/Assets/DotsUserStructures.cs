using Unity.Entities;
using Unity.Mathematics;


public struct UserComponentData : IComponentData
{
    public float2 Vector2Value;
    public int IntValue;
}

public readonly partial struct UserAspect : IAspect
{
    public readonly Entity Entity;
    private readonly RefRO<UserComponentData> _userComponentData;

    void Foo()
    {
        //typing_position
    }
}
