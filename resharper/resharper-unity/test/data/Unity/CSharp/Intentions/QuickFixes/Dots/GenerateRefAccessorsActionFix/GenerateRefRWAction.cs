using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Entities
{
    public interface IComponentData : IQueryTypeParameter { }
    public interface IAspect { }
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
}

namespace ComponentsAndTags
{
    struct Factory4 : IComponentData
    {
        public float2 ExitCoordinates;
        public int ScavsCount;
    }
    struct FooAspect : IAspect
    {
        private RefRW<Factory4> _factory{caret}4;
    }
}