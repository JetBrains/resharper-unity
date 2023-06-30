// ${KIND:Unity.GenerateRefAccessors}
// ${SELECT0:Value:System.Single}
// ${GLOBAL0:GenerateSetters=True}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    struct BrainHealth : IComponentData
    {
        public float Value;
        public float MaxValue;
    }
    struct FooAspect : IAspect
    {
        private RefRW<BrainHealth> _brain{caret}Health;
    }
}