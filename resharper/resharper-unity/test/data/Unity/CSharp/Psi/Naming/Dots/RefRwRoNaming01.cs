using System;
using UnityEngine;
using Unity.Entities;

namespace Unity.Entities
{
    public readonly struct RefRW<T>  {}
    public readonly struct RefRO<T>  {}
    public interface IAspect {}
    public interface IComponentData {}
}


public struct AnimationComponent : IComponentData
{

}

public readonly struct PlayerAspect : IAspect
{
    private readonly RefRO<AnimationComponent> {caret}
}