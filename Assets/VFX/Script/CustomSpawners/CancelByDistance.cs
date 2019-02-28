using UnityEngine;
using UnityEngine.Experimental.VFX;

public class CancelByDistance : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        [Tooltip("Position that will be compared to the \"position\" EventAttribute")]
        public Vector3 CheckPosition = Vector3.zero;
        [Tooltip("Distance from which the spawn will be canceled")]
        public float MaxDistance = 32.0f;
        [Tooltip("Invert Check : if true will cancel particles that are closer than the MaxDistance instead of those farther")]
        public bool InvertCheck = false;
    }

    static readonly int AttribPositionID    = Shader.PropertyToID("position");
    static readonly int CheckPositionID     = Shader.PropertyToID("CheckPosition");
    static readonly int MaxDistanceID       =   Shader.PropertyToID("MaxDistance");
    static readonly int InvertCheckID       =   Shader.PropertyToID("InvertCheck");

    public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {

    }

    public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {

    }

    public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        Vector3 checkPosition = vfxValues.GetVector3(CheckPositionID);
        float maxDistance = vfxValues.GetFloat(MaxDistanceID);
        bool InvertCheck = vfxValues.GetBool(InvertCheckID);
        Vector3 position = state.vfxEventAttribute.GetVector3(AttribPositionID);

        bool test = (checkPosition - position).sqrMagnitude > (maxDistance*maxDistance);

        if (test != InvertCheck)
        {
            state.spawnCount = 0;
        }

    }
}
