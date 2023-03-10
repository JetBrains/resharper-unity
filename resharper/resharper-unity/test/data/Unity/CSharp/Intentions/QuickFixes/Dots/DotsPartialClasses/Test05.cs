using Unity.Entities;

public struct BotData{caret}Aspect : IAspect
{
}

public readonly struct IKAnimationAspect : IAspect
{
}

public struct MyJobEntity : IJobEntity
{
}

struct PlayersDialogSystem : ISystem
{
    public void OnUpdate(ref SystemState state){}
    public void OnCreate(ref SystemState state){}
    public void OnDestroy(ref SystemState state){}
}
