// ${KIND:Unity.GenerateBakerAndAuthoring}
// ${SELECT0:SpawnRandom:Unity.Mathematics.Random}
// ${GLOBAL0:SelectedBaker=ComponentsAndTags.GraveyardPropertiesAuthoring+GraveyardPropertiesBaker}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    struct GraveyardProperties : IComponentData
    {
        public float2 FieldDimensions;
        public int NumberTombstonesToSpawn;
        public Entity TombstonePrefab;
    }

    struct Random{caret}Properties : IComponentData
    {
        public Unity.Mathematics.Random SpawnRandom;
    }

    public class GraveyardPropertiesAuthoring : MonoBehaviour
    {
        public string TombstonePrefab;
        public int NumberTombstonesToSpawn;

        public class GraveyardPropertiesBaker : Baker<GraveyardPropertiesAuthoring>
        {
            public override void Bake(GraveyardPropertiesAuthoring authoring)
            {
                AddComponent(new GraveyardProperties
                {
                    NumberTombstonesToSpawn = authoring.NumberTombstonesToSpawn,
                    TombstonePrefab = GetEntity(authoring.TombstonePrefab)
                });
            }
        }
    }
}