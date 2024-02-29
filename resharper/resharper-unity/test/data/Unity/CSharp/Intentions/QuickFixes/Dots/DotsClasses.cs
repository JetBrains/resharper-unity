using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Entities
{
    [Flags]
    public enum TransformUsageFlags : int
    {
        None = 0,
        Renderable = 1,
        Dynamic = 1 << 1,
        WorldSpace = 1 << 2,
        NonUniformScale = 1 << 3,
        ManualOverride = 1 << 4,
    }
    
    [Flags]
    public enum EntityQueryOptions
    {
        Default = 0,
        IncludePrefab = 1,
        IncludeDisabledEntities = 2,
        [Obsolete("This enum value has been renamed to IncludeDisabledEntities. (RemovedAfter Entities 1.0) (UnityUpgradable) -> IncludeDisabledEntities", false)] IncludeDisabled = IncludeDisabledEntities, // 0x00000002
        FilterWriteGroup = 4,
        IgnoreComponentEnabledState = 8,
        IncludeSystems = 16, // 0x00000010
    }

    public unsafe struct EntityQuery : IDisposable, IEquatable<EntityQuery>
    {
        public void Dispose() { }
        public bool Equals(EntityQuery other) { return true; }
    }


    public unsafe ref struct SystemState
    {
        public void RequireForUpdate<T>() {}
        public void RequireForUpdate(EntityQuery query) {}
    }
    
    public struct Entity {}

    public abstract class IBaker
    {
        public Entity GetEntity(TransformUsageFlags flags)
        {
            return new Entity();
        }

        public void AddComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData {}
        public void AddComponent<T>(Entity entity) {}
        public void AddComponentObject<T>(T component) where T : class {}
        public void AddComponentObject<T>(Entity entity, T component) where T : class {} 
    }
    
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
        public static T GetSingleton<T>() where T : unmanaged, IComponentData => throw new InvalidOperationException();
        public static SystemAPIQueryBuilder QueryBuilder() {return new SystemAPIQueryBuilder(); }
        public static QueryEnumerable<T1> Query<T1>() where T1 : IQueryTypeParameter => throw new InvalidOperationException();
        public static QueryEnumerable<T1, T2> Query<T1, T2>() where T1 : IQueryTypeParameter where T2 : IQueryTypeParameter => throw new InvalidOperationException();
        public static QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>() where T1 : IQueryTypeParameter where T2 : IQueryTypeParameter where T3 : IQueryTypeParameter => throw new InvalidOperationException();
        public static QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>() where T1 : IQueryTypeParameter where T2 : IQueryTypeParameter where T3 : IQueryTypeParameter where T4 : IQueryTypeParameter => throw new InvalidOperationException();
        public static QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>() where T1 : IQueryTypeParameter where T2 : IQueryTypeParameter where T3 : IQueryTypeParameter where T4 : IQueryTypeParameter where T5 : IQueryTypeParameter => throw new InvalidOperationException();
        public static QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>() where T1 : IQueryTypeParameter where T2 : IQueryTypeParameter where T3 : IQueryTypeParameter where T4 : IQueryTypeParameter where T5 : IQueryTypeParameter where T6 : IQueryTypeParameter => throw new InvalidOperationException();
        public static QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>() where T1 : IQueryTypeParameter where T2 : IQueryTypeParameter where T3 : IQueryTypeParameter where T4 : IQueryTypeParameter where T5 : IQueryTypeParameter where T6 : IQueryTypeParameter where T7 : IQueryTypeParameter => throw new InvalidOperationException();
    }

    public readonly struct RefRW<T> : IQueryTypeParameter where T : struct, IComponentData
    {
        private readonly T _data;
        public unsafe ref readonly T ValueRO {get {return ref _data; }}
        public unsafe ref readonly T ValueRW {get {return ref _data; }}
    }
    public readonly struct RefRO<T> : IQueryTypeParameter where T : struct, IComponentData
    {
        private readonly T _data;
        public unsafe ref readonly T ValueRO {get {return ref _data; }}
    }

    public readonly struct EnabledRefRO<T> : IQueryTypeParameter where T : unmanaged, IEnableableComponent
    {
        public bool ValueRO =>  true;
    }

    public readonly struct EnabledRefRW<T> : IQueryTypeParameter where T : unmanaged, IEnableableComponent
    {
        public bool ValueRO =>  true;
        public bool ValueRW
        {
            get => true;
            set { }
        }
    }

    public struct SystemAPIQueryBuilder
    {
        public SystemAPIQueryBuilder WithAll<T1>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAll<T1, T2>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAll<T1, T2, T3>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAll<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAllRW<T1>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAllRW<T1, T2>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1, T2>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1, T2, T3>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAny<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAnyRW<T1>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithAnyRW<T1, T2>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1, T2>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1, T2, T3>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4, T5>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4, T5, T6>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithNone<T1, T2, T3, T4, T5, T6, T7>() => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder WithOptions(EntityQueryOptions options) => throw ThrowNotBuildException();
        public SystemAPIQueryBuilder AddAdditionalQuery() => throw ThrowNotBuildException();
        public EntityQuery Build() => throw ThrowNotBuildException();
        static InvalidOperationException ThrowNotBuildException() => throw new InvalidOperationException("Source-generation will not run unless `.Build()` is invoked.");
    }


    public struct QueryEnumerable<T1> : IEnumerable<T1> { public IEnumerator<T1> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    public struct QueryEnumerable<T1, T2> : IEnumerable<(T1, T2)> { public IEnumerator<(T1, T2)> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    public struct QueryEnumerable<T1, T2, T3> : IEnumerable<(T1, T2, T3)> { public IEnumerator<(T1, T2, T3)> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    public struct QueryEnumerable<T1, T2, T3, T4> : IEnumerable<(T1, T2, T3, T4)> { public IEnumerator<(T1, T2, T3, T4)> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    public struct QueryEnumerable<T1, T2, T3, T4, T5> : IEnumerable<(T1, T2, T3, T4, T5)> { public IEnumerator<(T1, T2, T3, T4, T5)> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    public struct QueryEnumerable<T1, T2, T3, T4, T5, T6> : IEnumerable<(T1, T2, T3, T4, T5, T6)> { public IEnumerator<(T1, T2, T3, T4, T5, T6)> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    public struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> : IEnumerable<(T1, T2, T3, T4, T5, T6, T7)> { public IEnumerator<(T1, T2, T3, T4, T5, T6, T7)> GetEnumerator() => throw new NotImplementedException(); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }

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
        public void Dispose() => throw new NotImplementedException();
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        public bool Equals(NativeArray<T> other) => throw new NotImplementedException();
    }
}
