

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    public class GraveyardPropertiesBaker : Baker<GraveyardPropertiesAuthoring>
    {
        public override void Bake(GraveyardPropertiesAuthoring authoring)
        {
            AddComponent(new GraveyardProperties
            {
                NumberTombstonesToSpawn = authoring.NumberTombstonesToSpawn
            });
            
            float f = 1.0f + 41.0f //existing code must be saved
        }
    }
}