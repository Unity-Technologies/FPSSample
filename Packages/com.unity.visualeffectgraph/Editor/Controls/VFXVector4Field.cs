using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class VFXVector4Field : VFXVectorNField<Vector4>
    {
        protected override  int componentCount {get {return 4; }}
        protected override void SetValueComponent(ref Vector4 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                case 2:
                    value.z = componentValue;
                    break;
                default:
                    value.w = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector4 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                case 2:
                    return value.z;
                default:
                    return value.w;
            }
        }
    }
}
