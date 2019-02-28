using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using VFXVector2Field = UnityEditor.VFX.UIElements.VFXVector2Field;
using VFXVector4Field = UnityEditor.VFX.UIElements.VFXVector4Field;

namespace UnityEditor.VFX.UI
{
    class EnumPropertyRM : SimplePropertyRM<int>
    {
        public EnumPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public override ValueControl<int> CreateField()
        {
            return new EnumField(m_Label, m_Provider.portType);
        }
    }

    class Vector4PropertyRM : SimpleVFXUIPropertyRM<VFXVector4Field, Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 180;
        }
    }

    class Matrix4x4PropertyRM : SimpleVFXUIPropertyRM<VFXMatrix4x4Field, Matrix4x4>
    {
        public Matrix4x4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 260;
        }
    }

    class Vector2PropertyRM : SimpleVFXUIPropertyRM<VFXVector2Field, Vector2>
    {
        public Vector2PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }

    class FlipBookPropertyRM : SimpleVFXUIPropertyRM<VFXFlipBookField, FlipBook>
    {
        public FlipBookPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }
}
