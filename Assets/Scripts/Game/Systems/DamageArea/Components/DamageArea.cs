using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[ServerOnlyComponent]
[RequireComponent(typeof(BoxCollider))]
public class DamageArea : MonoBehaviour
{
    public bool instantKill = false;
    public float hitsPerSecond = 3;
    public float damagePerHit = 25;

    [NonSerialized]
    public List<CharacterInfo> charactersInside = new List<CharacterInfo>();

    public struct CharacterInfo
    {
        public Entity hitCollisionOwner;
        public int nextDamageTick;
    }

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("damagearea");
    }

    void OnTriggerEnter(Collider c)
    {
        var hitCollision = c.gameObject.GetComponent<HitCollision>();
        if (hitCollision == null)
            return;

        charactersInside.Add(new CharacterInfo { hitCollisionOwner = hitCollision.owner, nextDamageTick = 0 });
    }

    void OnTriggerExit(Collider c)
    {
        var hitCollision = c.gameObject.GetComponent<HitCollision>();
        if (hitCollision == null)
            return;
        
        for (var i = 0; i < charactersInside.Count; i++)
        {
            if (charactersInside[i].hitCollisionOwner == hitCollision.owner)
            {
                charactersInside.EraseSwap(i);
                break;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        if (gameObject == UnityEditor.Selection.activeGameObject)
        {
            // If we are directly selected (and not just our parent is selected)
            // draw with negative size to get an 'inside out' cube we can see from the inside
            Gizmos.color = new Color(1.0f, 1.0f, 0.5f, 0.8f);
            Gizmos.DrawCube(c.center, -c.size);
        }
        Gizmos.color = new Color(1.0f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawCube(c.center, c.size);
    }
#endif
}
