using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;


public abstract class AnimGraphAsset : ScriptableObject 
{
    public abstract IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner);
}

