// ${KIND:Unity.GenerateBakerAndAuthoring}
// ${GLOBAL0:SelectedBaker=ComponentsAndTags.GraveyardPropertiesBaker}

using Unity.Entities;
using Unity.Mathematics;

namespace ComponentsAndTags
{
    struct Graveyard{caret}Properties : IComponentData
    {
    }

    public class GraveyardPropertiesAuthoring : MonoBehaviour
    {
    }

    public class GraveyardPropertiesBaker : Baker<GraveyardPropertiesAuthoring>
    {
      public override void Bake(GraveyardPropertiesAuthoring authoring)
      {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<GraveyardProperties>(entity);
      }
    }
}