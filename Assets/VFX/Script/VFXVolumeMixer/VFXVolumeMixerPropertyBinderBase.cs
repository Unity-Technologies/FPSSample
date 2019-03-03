using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.VFX.Utils;

public abstract class VFXVolumeMixerPropertyBinderBase : VFXBinderBase
{
    public enum VolumeTrigger
    {
        SelfTransform = 0,
        MainCamera = 1,
        CustomTransform =2
    }
    [SerializeField]
    protected VolumeTrigger Trigger;
    [SerializeField]
    protected LayerMask Layer;
    [SerializeField]
    protected Transform CustomTransform;
    [SerializeField, Tooltip("If Trigger set To MainCamera, Use SceneCamera in Editor to preview instead of MainCamera")]
    protected bool PreviewSceneCamera = true;

    protected Transform computedTransform
    {
        get 
        {
            switch(Trigger)
            {
                default:
                case VolumeTrigger.SelfTransform: return gameObject.transform;
                case VolumeTrigger.MainCamera:
#if UNITY_EDITOR
                    if (Application.isEditor && !Application.isPlaying)
                        return UnityEditor.SceneView.lastActiveSceneView.camera.transform;
                    else
#endif
                    {
                        if (Camera.main != null)
                            return Camera.main.transform;
                        else
                            return null;
                    }

                case VolumeTrigger.CustomTransform: return CustomTransform;
            }
        }
    }

    public override bool IsValid(VisualEffect component)
    {
        return computedTransform != null;
    }

    public override string ToString()
    {
        return "(" + Trigger + ")";
    }

}
