using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="FPS Sample/Animation/SkeletonDefinition", fileName="SkeletonDefinition")]
public class SkeletonDefinition : ScriptableObject
{
    public Avatar avatar;
    public AvatarMask animationMask;
    public TextAsset tPose;
    public TextAsset bindPose;
    public GameObject colliders;
}



