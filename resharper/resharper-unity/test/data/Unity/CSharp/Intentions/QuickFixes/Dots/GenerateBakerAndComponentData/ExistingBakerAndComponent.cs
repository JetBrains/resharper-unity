// ${KIND:Unity.GenerateBakerAndComponent}
// ${SELECT0:ScavsCount:System.Int32}
// ${SELECT1:FieldDimensions:Unity.Mathematics.float2}
// ${GLOBAL0:SelectedComponent=ComponentsAndTags.GraveyardPropertiesComponentData}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    public class Graveyard{caret}PropertiesAuthoring : MonoBehaviour
    {
        public int ScavsCount;
        public float2 FieldDimensions;
    }

    public class GraveyardPropertiesAuthoringBaker : Baker<GraveyardPropertiesAuthoring>
    {
        public override void Bake(GraveyardPropertiesAuthoring authoring)
        {
            AddComponent(new GraveyardPropertiesComponentData { FieldDimensions = authoring.FieldDimensions });
        }
    }

    public struct GraveyardPropertiesComponentData : IComponentData
    {
        public float2 FieldDimensions;
    }
}