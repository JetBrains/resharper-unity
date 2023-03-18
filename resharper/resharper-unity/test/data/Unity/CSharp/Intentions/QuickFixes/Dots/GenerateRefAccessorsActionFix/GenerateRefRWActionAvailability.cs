using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    struct Factory4 : IComponentData
    {
        public float2 ExitCoordinates;
        public int ScavsCount;
    }
    struct FooAspect : IAspect
    {
        private RefRW<Factory4> _factory{on}4;
    }
}