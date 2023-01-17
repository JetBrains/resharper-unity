// ${KIND:Unity.GenerateBakerAndAuthoring}
// ${SELECT0:NumberTombstonesToSpawn:System.Int32}
// ${SELECT1:TombstonePrefab:Unity.Entities.Entity}

using Unity.Entities;
using Unity.Mathematics;

namespace ComponentsAndTags
{
    struct Graveyard{caret}Properties : IComponentData
    {
        public float2 FieldDimensions;
        public int NumberTombstonesToSpawn;
        public Entity TombstonePrefab;
    }
}