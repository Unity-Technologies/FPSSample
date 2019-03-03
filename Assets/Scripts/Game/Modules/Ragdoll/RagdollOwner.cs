using System;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(Skeleton))]  
public class RagdollOwner : MonoBehaviour
{
    public enum Phase
    {
        Inactive,
        PoseSampled,
        Active,
    }
    public Phase phase = Phase.Inactive;

    [NonSerialized] public float timeUntilStart = 0.0f;

    [Tooltip("The skeleton group of a Ragdoll Prefab")]
    public GameObject m_RagdollPrefab;
    public GameObject ragdollInstance;

    public Skeleton ragdollSkeleton;
    public Transform[] targeteBones;
    public Vector3[] lastBonePositions;
    public Quaternion[] lastBoneRotations;
}


[DisableAutoCreation]
public class HandleRagdollSpawn : InitializeComponentSystem<RagdollOwner>
{
    public HandleRagdollSpawn(GameWorld gameWorld, GameObject systemRoot) : base(gameWorld)
    {
        m_SystemRoot = systemRoot;
    }

    protected override void Initialize(Entity entity, RagdollOwner ragdoll)
    {
        // Create ragdoll instance
        ragdoll.ragdollInstance = GameObject.Instantiate(ragdoll.m_RagdollPrefab);
        ragdoll.ragdollInstance.SetActive(false);
        ragdoll.ragdollInstance.name = ragdoll.gameObject.name + "_Ragdoll";
            
        if(m_SystemRoot != null)
            ragdoll.ragdollInstance.transform.SetParent(m_SystemRoot.transform);

        ragdoll.ragdollSkeleton = ragdoll.ragdollInstance.GetComponent<Skeleton>();

        Skeleton targetSkeleton = ragdoll.GetComponent<Skeleton>();

        int boneCount = ragdoll.ragdollSkeleton.bones.Length;
        ragdoll.targeteBones = new Transform[boneCount];
        ragdoll.lastBonePositions = new Vector3[boneCount];
        ragdoll.lastBoneRotations = new Quaternion[boneCount];

        for (var i = 0; i < ragdoll.ragdollSkeleton.bones.Length; i++)
        {
            var targetBoneIndex = targetSkeleton.GetBoneIndex(ragdoll.ragdollSkeleton.nameHashes[i]);
            if (targetBoneIndex == -1)
            {
//                    GameDebug.LogError("Ragdoll bone could not be mapped. Bone name:" + ragdoll.ragdollSkeleton.m_Bones[i].name);
                continue;
            }

            ragdoll.targeteBones[i] = targetSkeleton.bones[targetBoneIndex];
        }
    }

    readonly GameObject m_SystemRoot;
}


[DisableAutoCreation]
public class HandleRagdollDespawn : DeinitializeComponentSystem<RagdollOwner>
{
    public HandleRagdollDespawn(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Deinitialize(Entity entity, RagdollOwner ragdoll)
    {
        GameObject.Destroy(ragdoll.ragdollInstance);
    }
}


[DisableAutoCreation]
public class UpdateRagdolls : BaseComponentSystem<CharacterPresentationSetup, RagdollOwner>
{
    public UpdateRagdolls(GameWorld gameWorld) : base(gameWorld) {}
    
    protected override void Update(Entity entity, CharacterPresentationSetup charPresentation, RagdollOwner ragdollOwner)
    {
        GameDebug.Assert(ragdollOwner.ragdollInstance != null, "Ragdoll instance is NULL for object: {0}", ragdollOwner.gameObject);
        GameDebug.Assert(EntityManager.Exists(charPresentation.character), "CharPresentation character does not exist");
        GameDebug.Assert(EntityManager.HasComponent<RagdollStateData>(charPresentation.character), "CharPresentation character does not have RagdollState");

        var ragdollState = EntityManager.GetComponentData<RagdollStateData>(charPresentation.character);
        if (ragdollState.ragdollActive == 0)
            return;
        
        switch (ragdollOwner.phase)
        {
            case RagdollOwner.Phase.Inactive:

                ragdollOwner.timeUntilStart -= m_world.frameDuration;
                
                if(ragdollOwner.timeUntilStart <= m_world.worldTime.tickInterval)
                {
                    // Store bone transforms so they can be used to calculate bone velocity next frame
                    for (int boneIndex = 0; boneIndex < ragdollOwner.targeteBones.Length; boneIndex++)
                    {
                        if (ragdollOwner.targeteBones[boneIndex] == null)
                            continue;

                        ragdollOwner.lastBonePositions[boneIndex] = ragdollOwner.targeteBones[boneIndex].position;
                        ragdollOwner.lastBoneRotations[boneIndex] = ragdollOwner.targeteBones[boneIndex].rotation;
                    }

                    ragdollOwner.phase = RagdollOwner.Phase.PoseSampled;
                }

                break;
            case RagdollOwner.Phase.PoseSampled:

                IntializeRagdoll(ragdollOwner, true, ragdollState.impulse);
                ragdollOwner.phase = RagdollOwner.Phase.Active;

                break;
            case RagdollOwner.Phase.Active:
                
                for (int boneIndex = 0; boneIndex < ragdollOwner.targeteBones.Length; boneIndex++)
                {
                    if (ragdollOwner.targeteBones[boneIndex] == null)
                        continue;

                    ragdollOwner.targeteBones[boneIndex].position = ragdollOwner.ragdollSkeleton.bones[boneIndex].position;
                    ragdollOwner.targeteBones[boneIndex].rotation = ragdollOwner.ragdollSkeleton.bones[boneIndex].rotation;
                }
                break;
        }
    }

    void IntializeRagdoll(RagdollOwner ragdollOwner, bool useAnimSpeed, Vector3 impulse)
    {
        ragdollOwner.ragdollInstance.SetActive(true);

        // Setup ragdoll
        var invFrameTime = 1.0f / m_world.frameDuration;
        for (int boneIndex = 0; boneIndex < ragdollOwner.targeteBones.Length; boneIndex++)
        {
            if (ragdollOwner.targeteBones[boneIndex] == null)
                continue;

            // Set start position
            var position = ragdollOwner.targeteBones[boneIndex].position;
            var rotation = ragdollOwner.targeteBones[boneIndex].rotation;
            ragdollOwner.ragdollSkeleton.bones[boneIndex].position = position;
            ragdollOwner.ragdollSkeleton.bones[boneIndex].rotation = rotation;

            // Set bone velocity
            var rigidBody = ragdollOwner.ragdollSkeleton.bones[boneIndex].GetComponent<Rigidbody>();
            if (rigidBody == null) 
                continue;   

            if (useAnimSpeed)
            {
                var torque = (Quaternion.Inverse(ragdollOwner.lastBoneRotations[boneIndex]) * rotation).eulerAngles * invFrameTime;
                rigidBody.AddTorque(torque, ForceMode.VelocityChange);
            }
            
            var velocity = Vector3.zero;
            //velocity += (position - ragdollOwner.lastBonePositions[boneIndex]) * invFrameTime;
            velocity += impulse; 
            rigidBody.AddForce(velocity, ForceMode.VelocityChange);
        }
   
    }
}


