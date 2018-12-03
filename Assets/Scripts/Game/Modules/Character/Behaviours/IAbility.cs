using UnityEngine;
using Unity.Entities;

public abstract class AbilityUI : MonoBehaviour    // TODO (mogensh) We should get rid of this a just use charPresentation prefabs to setup UI (same as char and items) 
{
    public Entity abilityOwner;
    
    public abstract void UpdateAbilityUI(EntityManager entityManager, ref GameTime time);
}

