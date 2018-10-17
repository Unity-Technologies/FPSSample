using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[ServerOnlyComponent]
public class TeleporterServer : MonoBehaviour
{
    public TeleporterServer targetTeleporter;
    public Vector3 spawnPosition;
    public SpatialEffectTypeDefinition effect;

    [System.NonSerialized]
    public HitCollision characterInside;

    void OnTriggerStay(Collider c)
    {
        // System is responsible for removing characters
        if(characterInside == null)
            characterInside = c.gameObject.GetComponent<HitCollision>();
    }

    public Vector3 GetSpawnPositionWorld()
    {
        return transform.TransformPoint(spawnPosition);
    }

    public Quaternion GetSpawnRotationWorld()
    {
        var q = Quaternion.identity;
        q.SetLookRotation(transform.TransformDirection(spawnPosition.normalized));
        return q;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.TransformPoint(spawnPosition) + Vector3.up * 0.10f, Vector3.one * 0.2f);
    }
#endif
}
