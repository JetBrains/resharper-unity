// ${KIND:Unity.GenerateBakerAndComponent}
// ${SELECT0:ScavsCount:System.Int32}
// ${GLOBAL0:SelectedComponent=ComponentsAndTags.GraveyardPropertiesAuthoringComponentData}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    public class Graveyard{caret}PropertiesAuthoring : MonoBehaviour
    {
        public int ScavsCount;
    }

    public struct GraveyardPropertiesAuthoringComponentData : IComponentData
    {
        public float2 FieldDimensions;
        public float2 ScavsCount;
    }
}