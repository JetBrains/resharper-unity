using Unity.Entities;

namespace Unity.Entities
{
    public interface IJobEntity {}

    public struct LocalTransform
    {
        public static readonly LocalTransform Identity = new LocalTransform();
    }
}

public partial struct InitLocalTransformBuildingGraphicPart : IJobEntity
{
    private void Execute(ref LocalTransform localTransform)
    {
        localTransform = LocalTransform.Identity;
    }
}
