using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.VFX;
using UnityEngine.VFX.Utils;

[Serializable]
public class VisualEffectActivationBehaviour : PlayableBehaviour
{
    [Serializable]
    public enum AttributeType
    {
        //Actually enum values are synchronized with VFXValueType
        Float = 1,
        Float2 = 2,
        Float3 = 3,
        Float4 = 4,
        Int32 = 5,
        Uint32 = 6,
        Boolean = 17
    }

    [Serializable]
    public struct EventState
    {
        public ExposedParameter attribute;
        public AttributeType type;
        public float[] values; //double could cover precision of integer and float within the same container, but not needed for now
    }

    [SerializeField]
    private ExposedParameter onClipEnter = "OnPlay";
    [SerializeField]
    private ExposedParameter onClipExit = "OnStop";
    [SerializeField]
    private EventState[] clipEnterEventAttributes;
    [SerializeField]
    private EventState[] clipExitEventAttributes;

    public override void OnPlayableCreate(Playable playable)
    {
    }

    //Potentially, BuildEventAttribute can be precomputed and stored as cached value in OnPlayableCreate
    public void SendEventEnter(VisualEffect component)
    {
        var evt = BuildEventAttribute(component, clipEnterEventAttributes);
        component.SendEvent(onClipEnter, evt);
    }

    public void SendEventExit(VisualEffect component)
    {
        var evt = BuildEventAttribute(component, clipExitEventAttributes);
        component.SendEvent(onClipExit, evt);
    }

    static private VFXEventAttribute BuildEventAttribute(VisualEffect component, EventState[] states)
    {
        if (states == null || states.Length == 0)
            return null;

        var evt = component.CreateVFXEventAttribute();
        foreach (var state in states)
        {
            switch (state.type)
            {
                case AttributeType.Float: evt.SetFloat(state.attribute, (float)state.values[0]); break;
                case AttributeType.Float2: evt.SetVector2(state.attribute, new Vector2((float)state.values[0], (float)state.values[1])); break;
                case AttributeType.Float3: evt.SetVector3(state.attribute, new Vector3((float)state.values[0], (float)state.values[1], (float)state.values[2])); break;
                case AttributeType.Float4: evt.SetVector4(state.attribute, new Vector4((float)state.values[0], (float)state.values[1], (float)state.values[2], (float)state.values[3])); break;
                case AttributeType.Int32: evt.SetInt(state.attribute, (int)state.values[0]); break;
                case AttributeType.Uint32: evt.SetUint(state.attribute, (uint)state.values[0]); break;
                case AttributeType.Boolean: evt.SetBool(state.attribute, state.values[0] != 0.0f); break;
            }
        }
        return evt;
    }
}
