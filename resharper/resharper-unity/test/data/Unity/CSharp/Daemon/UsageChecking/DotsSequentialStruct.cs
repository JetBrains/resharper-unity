using Unity.Entities;

namespace Unity.Entities
{
    public interface IJobEntity {}
}

public partial struct InitLocalTransformBuildingGraphicPart : IJobEntity
{
    public int A;
    public int B;
}

public partial struct InitLocalTransformBuildingGraphicPart
{
    public int C;
    public int D;
}
