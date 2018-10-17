using System;
using System.Net;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class CharacterItem1P : MonoBehaviour
{
    [NonSerialized] public Entity item;
    [NonSerialized] public Entity character1P;
}


[DisableAutoCreation]
public class CharacterItem1PLateUpdate : BaseComponentSystem<CharacterItem,CharacterItem1P>
{
    public CharacterItem1PLateUpdate(GameWorld world) : base(world) {}

    protected override void Update(Entity entity, CharacterItem item, CharacterItem1P item1P)
    {
        if (!EntityManager.Exists(item1P.character1P))
            return;
        
        var character1P = EntityManager.GetComponentObject<Character1P>(item1P.character1P);
            
        // Update visiblity
        item.SetVisible(character1P.isVisible);
        if (EntityManager.HasComponent<CharacterEvents>(item1P.character1P))
        {
            var footsteps = EntityManager.GetComponentObject<CharacterEvents>(item1P.character1P);
            footsteps.active = character1P.isVisible;
        }

        if (!item.visible)
            return;

        item1P.transform.position = character1P.itemAttachBone.position;
        item1P.transform.rotation = character1P.itemAttachBone.rotation;
    }
}

