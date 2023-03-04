// ${KIND:Unity.GenerateRefAccessors}
// ${SELECT0:PlayersCount():System.Int32}
// ${SELECT1:AICount():System.Int32}
// ${GLOBAL0:GenerateSetters=True}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ComponentsAndTags
{
    struct AIInfo : IComponentData
    {
        public int Count;
    }

    struct PlayersInfo : IComponentData
    {
        public int Count;
    }

    public readonly partial struct  FirstLevelAspect  : IAspect
    {
        private readonly RefRO<AIInfo> _aiInfo;
        private readonly RefRW<PlayersInfo> _playersInfo;

        public int PlayersCount
        {
            get => _playersInfo.ValueRO.Count;
            set => _playersInfo.ValueRW.Count = value;
        }

        public int AICount => _aiInfo.ValueRO.Count;

    }

    public readonly partial struct SecondLevelAspect : IAspect
    {
        public readonly FirstLevelAspect FirstLevel{caret}Aspect;
    }
}