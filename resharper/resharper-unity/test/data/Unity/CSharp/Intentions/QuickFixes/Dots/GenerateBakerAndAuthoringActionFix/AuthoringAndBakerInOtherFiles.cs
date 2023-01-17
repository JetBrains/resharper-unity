// ${KIND:Unity.GenerateBakerAndAuthoring}
// ${SELECT0:FieldDimensions:Unity.Mathematics.float2}
// ${SELECT1:TombstonePrefab:Unity.Entities.Entity}
// ${SELECT2:NumberTombstonesToSpawn:System.Int32}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    struct Graveyard{caret}Properties : IComponentData
    {
        public float2 FieldDimensions;
        public int NumberTombstonesToSpawn;
        public Entity TombstonePrefab;
    }
}