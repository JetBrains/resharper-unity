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
        {on}
    }
}