using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[DisableAutoCreation]
public class DamageAreaSystemServer : ComponentSystem
{
    ComponentGroup Group;

    public DamageAreaSystemServer(GameWorld gameWorld)
    {
        m_GameWorld = gameWorld;
    }

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(DamageArea));
    }
    
    protected override void OnUpdate()
    {
        var damageAreaArray = Group.GetComponentArray<DamageArea>();
        for (int idx = 0; idx < damageAreaArray.Length; ++idx)
        {
            var area = damageAreaArray[idx];
            var damageAmount = area.damagePerHit;
            var ticksBetweenDamage = Mathf.FloorToInt(1.0f / (area.hitsPerSecond * m_GameWorld.worldTime.tickInterval));
            if (area.instantKill)
                damageAmount = 100000.0f;
            var charactersInside = area.charactersInside;

            // Iterating backwards as we need to clear out any destroyed characters
            for (var i = charactersInside.Count - 1; i >= 0; --i)
            {
                if (charactersInside[i].hitCollisionOwner == null)
                {
                    charactersInside.EraseSwap(i);
                    continue;
                }
                if (m_GameWorld.worldTime.tick > charactersInside[i].nextDamageTick)
                {
                    charactersInside[i].hitCollisionOwner.damageEvents.Add(new DamageEvent(Entity.Null, damageAmount, Vector3.zero, 0));
                    var info = charactersInside[i];
                    info.nextDamageTick = m_GameWorld.worldTime.tick + ticksBetweenDamage;
                    charactersInside[i] = info;
                }
            }
        }
    }
    GameWorld m_GameWorld;
}
