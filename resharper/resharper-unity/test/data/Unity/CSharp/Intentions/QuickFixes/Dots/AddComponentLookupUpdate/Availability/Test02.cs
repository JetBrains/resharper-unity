using Unity.Entities;

struct Bar : IComponentData, IEnableableComponent
{
}

partial struct Foo : ISystem
{
    private ComponentLookup<Bar> NotUpdatedComponentLookup; //Must be marked with warning

    delegate void ActionRef<T>(ref T sys, ref SystemState state);

    public void OnUpdate(ref SystemState state)
    {
        ActionRef<Foo> action = (ref Foo sys, ref SystemState systemState) =>
        {
            sys.NotUpdatedComponentLookup.Update(ref systemState);
        };
        
        ActionRef<SafeZoneSystem> action2 = (ref SafeZoneSystem sys, ref SystemState systemState) =>
            sys.m_TurretActiveFromEntity.Update(ref systemState);
        
        var fff = this;
        void LocalMethod(ref SystemState state)
        {
            fff.m_TurretActiveFromEntity.Update(systemState: ref state);
        }
    }
}
