using Unity.Entities;

struct Killa : IComponentData, IEnableableComponent
{
}

struct Tagilla : IComponentData, IEnableableComponent
{
}

struct Chadilla : IComponentData, IEnableableComponent
{
}


struct Loldilla : IComponentData, IEnableableComponent
{
}

partial struct Foo : ISystem
{
    void Doo()
    {
        var k = SystemAPI.GetSingleton<Killa>();//Must be marked with warning
    }
}


partial struct Foo : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var t = SystemAPI.GetSingleton<Tagilla>();//Must be marked with warning

        var t = SystemAPI.GetSingleton<Loldilla>();//Must NOT be marked with warning RIDER-55779
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Chadilla>();
        var requiredLoldilla = SystemAPI.QueryBuilder().WithAll<Loldilla>().Build();
        state.RequireForUpdate(requiredLoldilla);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    private void CustomUpdate(ref SystemState state)
    {
        this.UpdatedByInternalMethod.Update(ref state);
    }
}
