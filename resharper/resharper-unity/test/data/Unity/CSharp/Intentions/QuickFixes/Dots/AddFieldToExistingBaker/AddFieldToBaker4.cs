using Unity.Entities;
using UnityEngine;

namespace Systems
{
  public struct Angle : IComponentData
  {
    public float Value;
    public float Max{caret}Value;
  }

  public class AngleAuthoring : MonoBehaviour
  {
    public float Angle;
    
    public class AngleBaker1 : Baker<AngleAuthoring>
    {
      public override void Bake(AngleAuthoring authoring)
      {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<Angle>(entity);
      }
    }

    public class AngleBaker2 : Baker<AngleAuthoring>
    {
      public override void Bake(AngleAuthoring authoring)
      {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<Angle>(entity);
      }
    }
  }
}
