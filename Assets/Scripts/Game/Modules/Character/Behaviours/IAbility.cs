using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public abstract class AbilityUI : MonoBehaviour 
{
    public Entity ability;
    
    public abstract void UpdateAbilityUI(EntityManager entityManager, ref GameTime time);
}


public abstract class AbilityAsset : ScriptableObject
{
    public abstract void AddAbilityComponents(EntityManager entityManager, Entity abilityEntity, 
        List<IPredictedDataHandler> predictedHandlers, List<IInterpolatedDataHandler> interpolatedHandlers);

    public virtual AbilityUI CreateAbilityUI(Entity abilityEntity)//  TODO (mogensh) move this from ability asset. Shuold not exist on server. More tied to item and presentation
    {
        return null;
    } 
}


