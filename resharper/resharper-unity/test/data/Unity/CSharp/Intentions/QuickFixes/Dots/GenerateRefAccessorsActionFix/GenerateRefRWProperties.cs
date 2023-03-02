// ${KIND:Unity.GenerateRefAccessors}
// ${SELECT0:ArrayOfIndexes:Unity.Collections.NativeArray`1[T -> System.Int32]}
// ${GLOBAL0:GenerateSetters=True}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

namespace ComponentsAndTags
{
    struct Factory4 : IComponentData
    {
        public float2 ExitCoordinates;
        public int ScavsCount;
        public NativeArray<int> ArrayOfIndexes;
    }

    struct FooAspect : IAspect
    {
        private RefRW<Factory4> _factory{caret}4;
    }
}