using System;
using UnityEngine;

[RequireComponent(typeof(DamageHistory))]
[RequireComponent(typeof(HealthState))]
[RequireComponent(typeof(CharacterAnimState))]
[RequireComponent(typeof(UserCommandComponent))]
[RequireComponent(typeof(AbilityController))]
[RequireComponent(typeof(RagdollState))]
[RequireComponent(typeof(HitCollisionOwner))]
[RequireComponent(typeof(AnimStateController))]
[RequireComponent(typeof(CharacterPredictedState))]
[RequireComponent(typeof(CharacterMoveQuery))]
public class Character : MonoBehaviour, INetworkSerializable
{
    public GameObject geomtry;
    public Transform itemAttachBone;
    
    public Transform weaponBoneDebug;
    public Vector3 weaponOffsetDebug;
    
    [NonSerialized] public bool isVisible = true;
    [NonSerialized] public float eyeHeight = 1.8f;
    [NonSerialized] public string characterName;
    [NonSerialized] public HealthState healthState;
    [NonSerialized] public int heroTypeIndex;
    [NonSerialized] public HeroTypeAsset heroTypeData;

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if(geomtry != null && geomtry.activeSelf != visible)  
            geomtry.SetActive(visible);
    }
    
    private void Awake()
    {
        healthState = GetComponent<HealthState>();
    }    
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt16("heroType",(short)heroTypeIndex);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        heroTypeIndex = reader.ReadInt16();
    }
}
