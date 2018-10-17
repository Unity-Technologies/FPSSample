using UnityEngine;
using System;
using Unity.Entities;
using UnityEngine.Profiling;

[RequireComponent(typeof(GameObjectEntity))]
public class CharacterPresentation : InterpolatedComponentDataBehavior<CharAnimState>
{
}



[DisableAutoCreation]
public class InterpolatePresentationState : InterpolatedComponentDataBehaviorInterpolate<CharacterPresentation,CharAnimState>
{
    public InterpolatePresentationState(GameWorld gameWorld) : base(gameWorld)
    {}
}
