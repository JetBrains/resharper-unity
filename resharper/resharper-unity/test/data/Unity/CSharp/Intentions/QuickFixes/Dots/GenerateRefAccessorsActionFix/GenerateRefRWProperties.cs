// ${KIND:Unity.GenerateRefAccessors}
// ${SELECT0:ScavsCount:System.Int32}
// ${GLOBAL0:GenerateSetters=True}

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
        private RefRW<Factory4> _factory{caret}4;
    }
}