using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering
{
    // We need this base class to be able to store a list of VolumeParameter in collections as we
    // can't store VolumeParameter<T> with variable T types in the same collection. As a result some
    // of the following is a bit hacky...
    public abstract class VolumeParameter
    {
        public const string k_DebuggerDisplay = "{m_Value} ({m_OverrideState})";

        [SerializeField]
        protected bool m_OverrideState;

        public virtual bool overrideState
        {
            get { return m_OverrideState; }
            set { m_OverrideState = value; }
        }

        internal abstract void Interp(VolumeParameter from, VolumeParameter to, float t);

        public T GetValue<T>()
        {
            return ((VolumeParameter<T>) this).value;
        }

        internal abstract void SetValue(VolumeParameter parameter);

        // This is used in case you need to access fields/properties that can't be accessed in the
        // constructor of a ScriptableObject (VolumeParameter are generally declared and inited in
        // a VolumeComponent which is a ScriptableObject). This will be called right after the
        // VolumeComponent object has been constructed, thus allowing access to previously
        // "forbidden" fields/properties.
        protected internal virtual void OnEnable()
        {
        }

        // Called when the parent VolumeComponent OnDisabled is called
        protected internal virtual void OnDisable()
        {
        }

        public static bool IsObjectParameter(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObjectParameter<>))
                return true;

            return type.BaseType != null
                && IsObjectParameter(type.BaseType);
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class VolumeParameter<T> : VolumeParameter, IEquatable<VolumeParameter<T>>
    {
        [SerializeField]
        protected T m_Value;

        public virtual T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected const float k_DefaultInterpSwap = 0f;

        public VolumeParameter()
            : this(default(T), false)
        {
        }

        protected VolumeParameter(T value, bool overrideState)
        {
            m_Value = value;
            this.overrideState = overrideState;
        }

        internal override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            // Note: this is relatively unsafe (assumes that from and to are both holding type T)
            Interp(from.GetValue<T>(), to.GetValue<T>(), t);
        }

        public virtual void Interp(T from, T to, float t)
        {
            // Default interpolation is naive
            m_Value = t > k_DefaultInterpSwap ? to : from;
        }

        public void Override(T x)
        {
            overrideState = true;
            m_Value = x;
        }

        internal override void SetValue(VolumeParameter parameter)
        {
            m_Value = parameter.GetValue<T>();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", value, overrideState);
        }

        public static bool operator==(VolumeParameter<T> lhs, T rhs)
        {
            return lhs != null && lhs.value != null && lhs.value.Equals(rhs);
        }

        public static bool operator!=(VolumeParameter<T> lhs, T rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(VolumeParameter<T> other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((VolumeParameter<T>)obj);
        }

        //
        // Implicit conversion; assuming the following:
        //
        //   var myFloatProperty = new ParameterOverride<float> { value = 42f; };
        //
        // It allows for implicit casts:
        //
        //   float myFloat = myFloatProperty.value; // No implicit cast
        //   float myFloat = myFloatProperty;       // Implicit cast
        //
        // For safety reason this is one-way only.
        //
        public static implicit operator T(VolumeParameter<T> prop)
        {
            return prop.m_Value;
        }
    }

    //
    // The serialization system in Unity can't serialize generic types, the workaround is to extend
    // and flatten pre-defined generic types.
    // For enums it's recommended to make your own types on the spot, like so:
    //
    //  [Serializable]
    //  public sealed class MyEnumParameter : VolumeParameter<MyEnum> { }
    //  public enum MyEnum { One, Two }
    //

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class BoolParameter : VolumeParameter<bool>
    {
        public BoolParameter(bool value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class IntParameter : VolumeParameter<int>
    {
        public IntParameter(int value, bool overrideState = false)
            : base(value, overrideState) {}

        public sealed override void Interp(int from, int to, float t)
        {
            // Int snapping interpolation. Don't use this for enums as they don't necessarily have
            // contiguous values. Use the default interpolator instead (same as bool).
            m_Value = (int)(from + (to - from) * t);
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpIntParameter : VolumeParameter<int>
    {
        public NoInterpIntParameter(int value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class MinIntParameter : IntParameter
    {
        public int min;

        public override int value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Max(value, min); }
        }

        public MinIntParameter(int value, int min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpMinIntParameter : VolumeParameter<int>
    {
        public int min;

        public override int value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Max(value, min); }
        }

        public NoInterpMinIntParameter(int value, int min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class MaxIntParameter : IntParameter
    {
        public int max;

        public override int value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Min(value, max); }
        }

        public MaxIntParameter(int value, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpMaxIntParameter : VolumeParameter<int>
    {
        public int max;

        public override int value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Min(value, max); }
        }

        public NoInterpMaxIntParameter(int value, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class ClampedIntParameter : IntParameter
    {
        public int min;
        public int max;

        public override int value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Clamp(value, min, max); }
        }

        public ClampedIntParameter(int value, int min, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpClampedIntParameter : VolumeParameter<int>
    {
        public int min;
        public int max;

        public override int value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Clamp(value, min, max); }
        }

        public NoInterpClampedIntParameter(int value, int min, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class FloatParameter : VolumeParameter<float>
    {
        public FloatParameter(float value, bool overrideState = false)
            : base(value, overrideState) {}

        public sealed override void Interp(float from, float to, float t)
        {
            m_Value = from + (to - from) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpFloatParameter : VolumeParameter<float>
    {
        public NoInterpFloatParameter(float value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class MinFloatParameter : FloatParameter
    {
        public float min;

        public override float value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Max(value, min); }
        }

        public MinFloatParameter(float value, float min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpMinFloatParameter : VolumeParameter<float>
    {
        public float min;

        public override float value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Max(value, min); }
        }

        public NoInterpMinFloatParameter(float value, float min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class MaxFloatParameter : FloatParameter
    {
        public float max;

        public override float value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Min(value, max); }
        }

        public MaxFloatParameter(float value, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpMaxFloatParameter : VolumeParameter<float>
    {
        public float max;

        public override float value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Min(value, max); }
        }

        public NoInterpMaxFloatParameter(float value, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class ClampedFloatParameter : FloatParameter
    {
        public float min;
        public float max;

        public override float value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Clamp(value, min, max); }
        }

        public ClampedFloatParameter(float value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpClampedFloatParameter : VolumeParameter<float>
    {
        public float min;
        public float max;

        public override float value
        {
            get { return m_Value; }
            set { m_Value = Mathf.Clamp(value, min, max); }
        }

        public NoInterpClampedFloatParameter(float value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    // Holds a min & a max values clamped in a range (MinMaxSlider in the editor)
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FloatRangeParameter : VolumeParameter<Vector2>
    {
        public float min;
        public float max;

        public override Vector2 value
        {
            get { return m_Value; }
            set
            {
                m_Value.x = Mathf.Max(value.x, min);
                m_Value.y = Mathf.Min(value.y, max);
            }
        }

        public FloatRangeParameter(Vector2 value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }

        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpFloatRangeParameter : VolumeParameter<Vector2>
    {
        public float min;
        public float max;

        public override Vector2 value
        {
            get { return m_Value; }
            set
            {
                m_Value.x = Mathf.Max(value.x, min);
                m_Value.y = Mathf.Min(value.y, max);
            }
        }

        public NoInterpFloatRangeParameter(Vector2 value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    // 32-bit RGBA
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class ColorParameter : VolumeParameter<Color>
    {
        public bool hdr = false;
        public bool showAlpha = true;
        public bool showEyeDropper = true;

        public ColorParameter(Color value, bool overrideState = false)
            : base(value, overrideState) {}

        public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false)
            : base(value, overrideState)
        {
            this.hdr = hdr;
            this.showAlpha = showAlpha;
            this.showEyeDropper = showEyeDropper;
            this.overrideState = overrideState;
        }

        public override void Interp(Color from, Color to, float t)
        {
            // Lerping color values is a sensitive subject... We looked into lerping colors using
            // HSV and LCH but they have some downsides that make them not work correctly in all
            // situations, so we stick with RGB lerping for now, at least its behavior is
            // predictable despite looking desaturated when `t ~= 0.5` and it's faster anyway.
            m_Value.r = from.r + (to.r - from.r) * t;
            m_Value.g = from.g + (to.g - from.g) * t;
            m_Value.b = from.b + (to.b - from.b) * t;
            m_Value.a = from.a + (to.a - from.a) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpColorParameter : VolumeParameter<Color>
    {
        public bool hdr = false;
        public bool showAlpha = true;
        public bool showEyeDropper = true;

        public NoInterpColorParameter(Color value, bool overrideState = false)
            : base(value, overrideState) {}

        public NoInterpColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false)
            : base(value, overrideState)
        {
            this.hdr = hdr;
            this.showAlpha = showAlpha;
            this.showEyeDropper = showEyeDropper;
            this.overrideState = overrideState;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class Vector2Parameter : VolumeParameter<Vector2>
    {
        public Vector2Parameter(Vector2 value, bool overrideState = false)
            : base(value, overrideState) {}

        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpVector2Parameter : VolumeParameter<Vector2>
    {
        public NoInterpVector2Parameter(Vector2 value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class Vector3Parameter : VolumeParameter<Vector3>
    {
        public Vector3Parameter(Vector3 value, bool overrideState = false)
            : base(value, overrideState) {}

        public override void Interp(Vector3 from, Vector3 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
            m_Value.z = from.z + (to.z - from.z) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpVector3Parameter : VolumeParameter<Vector3>
    {
        public NoInterpVector3Parameter(Vector3 value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class Vector4Parameter : VolumeParameter<Vector4>
    {
        public Vector4Parameter(Vector4 value, bool overrideState = false)
            : base(value, overrideState) {}

        public override void Interp(Vector4 from, Vector4 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
            m_Value.z = from.z + (to.z - from.z) * t;
            m_Value.w = from.w + (to.w - from.w) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpVector4Parameter : VolumeParameter<Vector4>
    {
        public NoInterpVector4Parameter(Vector4 value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class TextureParameter : VolumeParameter<Texture>
    {
        public TextureParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) {}

        // TODO: Texture interpolation
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpTextureParameter : VolumeParameter<Texture>
    {
        public NoInterpTextureParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class RenderTextureParameter : VolumeParameter<RenderTexture>
    {
        public RenderTextureParameter(RenderTexture value, bool overrideState = false)
            : base(value, overrideState) {}

        // TODO: RenderTexture interpolation
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpRenderTextureParameter : VolumeParameter<RenderTexture>
    {
        public NoInterpRenderTextureParameter(RenderTexture value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class CubemapParameter : VolumeParameter<Cubemap>
    {
        public CubemapParameter(Cubemap value, bool overrideState = false)
            : base(value, overrideState) {}

        // TODO: Cubemap interpolation
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class NoInterpCubemapParameter : VolumeParameter<Cubemap>
    {
        public NoInterpCubemapParameter(Cubemap value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    // Used as a container to store custom serialized classes/structs inside volume components
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class ObjectParameter<T> : VolumeParameter<T>
    {
        internal ReadOnlyCollection<VolumeParameter> parameters { get; private set; }

        // Force override state to true for container objects
        public override bool overrideState
        {
            get { return true; }
            set { m_OverrideState = true; }
        }

        public override T value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;

                if (m_Value == null)
                {
                    parameters = null;
                    return;
                }

                // Automatically grab all fields of type VolumeParameter contained in this instance
                parameters = m_Value.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                    .OrderBy(t => t.MetadataToken) // Guaranteed order
                    .Select(t => (VolumeParameter)t.GetValue(m_Value))
                    .ToList()
                    .AsReadOnly();
            }
        }

        public ObjectParameter(T value)
        {
            m_OverrideState = true;
            this.value = value;
        }

        internal override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            if (m_Value == null)
                return;

            var paramOrigin = parameters;
            var paramFrom = ((ObjectParameter<T>)from).parameters;
            var paramTo = ((ObjectParameter<T>)to).parameters;

            for (int i = 0; i < paramFrom.Count; i++)
            {
                if (paramOrigin[i].overrideState)
                    paramOrigin[i].Interp(paramFrom[i], paramTo[i], t);
            }
        }
    }
}
