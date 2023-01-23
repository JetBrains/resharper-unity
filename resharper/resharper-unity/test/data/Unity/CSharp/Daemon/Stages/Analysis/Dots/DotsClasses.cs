namespace Unity.Entities
{
    public unsafe ref struct SystemState
    {
        public void RequireForUpdate<T>() {}

    }
    public struct Entity {}

    public abstract class IBaker { }
    public abstract class Baker<TAuthoringType> : IBaker {}
    public interface IJobEntity { }
    public interface IAspect { }
    public interface IQueryTypeParameter { }
    public interface IComponentData : IQueryTypeParameter { }

    public unsafe struct ComponentLookup<T> where T : unmanaged, IComponentData
    {
        public void Update(ref SystemState system)
        {
        }
    }
    
    public interface IEnableableComponent { }

    public interface ISystem
    {

        void OnCreate(ref SystemState state);

        void OnDestroy(ref SystemState state);

       
        void OnUpdate(ref SystemState state);
    }

    public  abstract class SystemBase : ComponentSystemBase
    {
    }
    
    public abstract  partial class ComponentSystemBase {}

    public static class SystemAPI
    {
        public static T GetSingleton<T>()
            where T : unmanaged, IComponentData
        {
        }
    }
}

namespace Unity.Mathematics
{
    public struct float2 { }
}