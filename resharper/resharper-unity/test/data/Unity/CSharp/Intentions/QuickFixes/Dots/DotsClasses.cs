namespace Unity.Entities
{
    public unsafe ref struct SystemState
    {
        public void RequireForUpdate<T>() {}

    }
    public struct Entity {}

    public abstract class IBaker { }
    public abstract class Baker<TAuthoringType> : IBaker {}

    public interface IQueryTypeParameter { }
    public interface IComponentData : IQueryTypeParameter { }
    public interface IAspect { }

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

    public class ComponentSystemBase {}
    public class SystemBase : ComponentSystemBase
    {
        protected virtual void OnUpdate() {}
    }

    public static class SystemAPI
    {
        public static T GetSingleton<T>()
            where T : unmanaged, IComponentData
        {
        }
    }

    public readonly struct RefRW<T>
    {
        private T _data;
        public unsafe ref readonly T ValueRO {get {return _data; }}
        public unsafe ref readonly T ValueRW {get {return _data; }}
    }
    public readonly struct RefRO<T>
    {
        private T _data;
        public unsafe ref readonly T ValueRO {get {return _data; }}
    }
}

namespace Unity.Mathematics
{
    public struct float2 { }
    public struct Random
    {
        public static Random CreateFromIndex(uint index)
        {
            return new Random();
        }
    }
}


namespace Unity.Collections
{

    public struct NativeArray<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<NativeArray<T>>
        where T : struct
    {}
}
