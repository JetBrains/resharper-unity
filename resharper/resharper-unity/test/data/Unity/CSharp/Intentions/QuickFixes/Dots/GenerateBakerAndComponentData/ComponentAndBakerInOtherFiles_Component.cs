using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    public struct GraveyardPropertiesAuthoringComponentData : IComponentData
    {
        public float2 FieldDimensions;
    }
}