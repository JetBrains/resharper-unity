namespace Unity.Entities
{
    public unsafe ref struct SystemState
    {
    }
    
    public interface IQueryTypeParameter
    {
    }
    public interface IComponentData : IQueryTypeParameter
    {
    }

    public unsafe struct ComponentLookup<T> where T : unmanaged, IComponentData
    {
        public void Update(ref SystemState system)
        {
        }
    }
    
    public interface IEnableableComponent
    {
    }

    public interface ISystem
    {

        void OnCreate(ref SystemState state);

        void OnDestroy(ref SystemState state);

       
        void OnUpdate(ref SystemState state);
    }
}