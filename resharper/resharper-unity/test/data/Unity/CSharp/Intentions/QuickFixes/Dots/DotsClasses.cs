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
        public static T GetSingleton<T>()
            where T : unmanaged, IComponentData
        {
        }

        public static SystemAPIQueryBuilder QueryBuilder() {return new SystemAPIQueryBuilder(); }
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

    public readonly struct EnabledRefRO<T>
    {
        public bool ValueRO =>  true;
    }

    public readonly struct EnabledRefRW<T>
    {
        public bool ValueRW {get;set;};
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
        public EntityQuery Build() => throw InternalCompilerInterface.ThrowCodeGenException();
        static InvalidOperationException ThrowNotBuildException() => throw new InvalidOperationException("Source-generation will not run unless `.Build()` is invoked.");
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
