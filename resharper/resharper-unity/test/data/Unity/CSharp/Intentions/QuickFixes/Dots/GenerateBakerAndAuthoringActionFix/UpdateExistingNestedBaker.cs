// ${KIND:Unity.GenerateBakerAndAuthoring}
// ${SELECT0:FieldDimensions:Unity.Mathematics.float2}
// ${SELECT1:TombstonePrefab:Unity.Entities.Entity}
// ${GLOBAL0:GenerateAsNested=True}

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

    public class GraveyardPropertiesAuthoring : MonoBehaviour
    {
        public string TombstonePrefab; //Field with existing name in component
        public int NumberTombstonesToSpawn;

        public class GraveyardPropertiesBaker : Baker<GraveyardPropertiesAuthoring>
        {
            public override void Bake(GraveyardPropertiesAuthoring authoring)
            {
                AddComponent(new GraveyardProperties
                {
                    NumberTombstonesToSpawn = authoring.NumberTombstonesToSpawn,
                });
            }
        }
    }
}