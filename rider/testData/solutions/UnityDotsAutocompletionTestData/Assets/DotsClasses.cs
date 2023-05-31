using System;
using System.Collections;
using System.Collections.Generic;

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
    public interface IEnableableComponent {}
    public interface IAspect { }
    public interface IJobEntity {}

    public unsafe struct ComponentLookup<T> where T : unmanaged, IComponentData
    {
        public void Update(ref SystemState system)
        {
        }
    }
    
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
            return default;
        }
    }

    public readonly struct RefRW<T>
    {
        private readonly T _data;
        // public unsafe ref readonly T ValueRO {get {return ref _data; }}
        // public unsafe ref readonly T ValueRW {get {return ref _data; }}
    }
    public readonly struct RefRO<T>
    {
        private readonly T _data;
        // public unsafe ref readonly T ValueRO {get {return ref _data; }}
    }

    public readonly struct EnabledRefRO<T>
    {
        public bool ValueRO =>  true;
    }

    public readonly struct EnabledRefRW<T>
    {
        public bool ValueRW {get; }
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
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(NativeArray<T> other)
        {
            throw new NotImplementedException();
        }
    }
}
