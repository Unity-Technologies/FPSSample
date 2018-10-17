using System;

namespace UnityEngine.Rendering.PostProcessing
{
    public abstract class ParameterOverride
    {
        public bool overrideState;

        internal abstract void Interp(ParameterOverride from, ParameterOverride to, float t);

        public abstract int GetHash();

        public T GetValue<T>()
        {
            return ((ParameterOverride<T>)this).value;
        }

        // This is used in case you need to access fields/properties that can't be accessed in the
        // constructor of a ScriptableObject (ParameterOverride are generally declared and inited in
        // a PostProcessEffectSettings which is a ScriptableObject). This will be called right
        // after the settings object has been constructed, thus allowing previously "forbidden"
        // fields/properties.
        protected internal virtual void OnEnable()
        {
        }

        // Here for consistency reasons (cf. OnEnable)
        protected internal virtual void OnDisable()
        {
        }

        internal abstract void SetValue(ParameterOverride parameter);
    }

    [Serializable]
    public class ParameterOverride<T> : ParameterOverride
    {
        public T value;

        public ParameterOverride()
            : this(default(T), false)
        {
        }

        public ParameterOverride(T value)
            : this(value, false)
        {
        }

        public ParameterOverride(T value, bool overrideState)
        {
            this.value = value;
            this.overrideState = overrideState;
        }

        internal override void Interp(ParameterOverride from, ParameterOverride to, float t)
        {
            // Note: this isn't completely safe but it'll do fine
            Interp(from.GetValue<T>(), to.GetValue<T>(), t);
        }

        public virtual void Interp(T from, T to, float t)
        {
            // Returns `b` if `dt > 0` by default so we don't have to write overrides for bools and
            // enumerations.
            value = t > 0f ? to : from;
        }

        public void Override(T x)
        {
            overrideState = true;
            value = x;
        }

        internal override void SetValue(ParameterOverride parameter)
        {
            value = parameter.GetValue<T>();
        }

        public override int GetHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }

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
        public static implicit operator T(ParameterOverride<T> prop)
        {
            return prop.value;
        }
    }

    // Bypassing the limited unity serialization system...
    [Serializable]
    public sealed class FloatParameter : ParameterOverride<float>
    {
        public override void Interp(float from, float to, float t)
        {
            value = from + (to - from) * t;
        }
    }

    [Serializable]
    public sealed class IntParameter : ParameterOverride<int>
    {
        public override void Interp(int from, int to, float t)
        {
            // Int snapping interpolation. Don't use this for enums as they don't necessarily have
            // contiguous values. Use the default interpolator instead (same as bool).
            value = (int)(from + (to - from) * t);
        }
    }

    [Serializable]
    public sealed class BoolParameter : ParameterOverride<bool> {}

    [Serializable]
    public sealed class ColorParameter : ParameterOverride<Color>
    {
        public override void Interp(Color from, Color to, float t)
        {
            // Lerping color values is a sensitive subject... We looked into lerping colors using
            // HSV and LCH but they have some downsides that make them not work correctly in all
            // situations, so we stick with RGB lerping for now, at least its behavior is
            // predictable despite looking desaturated when `t ~= 0.5` and it's faster anyway.
            value.r = from.r + (to.r - from.r) * t;
            value.g = from.g + (to.g - from.g) * t;
            value.b = from.b + (to.b - from.b) * t;
            value.a = from.a + (to.a - from.a) * t;
        }

        public static implicit operator Vector4(ColorParameter prop)
        {
            return prop.value;
        }
    }

    [Serializable]
    public sealed class Vector2Parameter : ParameterOverride<Vector2>
    {
        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
        }

        public static implicit operator Vector3(Vector2Parameter prop)
        {
            return prop.value;
        }

        public static implicit operator Vector4(Vector2Parameter prop)
        {
            return prop.value;
        }
    }

    [Serializable]
    public sealed class Vector3Parameter : ParameterOverride<Vector3>
    {
        public override void Interp(Vector3 from, Vector3 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
            value.z = from.z + (to.z - from.z) * t;
        }

        public static implicit operator Vector2(Vector3Parameter prop)
        {
            return prop.value;
        }

        public static implicit operator Vector4(Vector3Parameter prop)
        {
            return prop.value;
        }
    }

    [Serializable]
    public sealed class Vector4Parameter : ParameterOverride<Vector4>
    {
        public override void Interp(Vector4 from, Vector4 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
            value.z = from.z + (to.z - from.z) * t;
            value.w = from.w + (to.w - from.w) * t;
        }

        public static implicit operator Vector2(Vector4Parameter prop)
        {
            return prop.value;
        }

        public static implicit operator Vector3(Vector4Parameter prop)
        {
            return prop.value;
        }
    }

    [Serializable]
    public sealed class SplineParameter : ParameterOverride<Spline>
    {
        protected internal override void OnEnable()
        {
            if (value != null)
                value.Cache(int.MinValue);
        }

        internal override void SetValue(ParameterOverride parameter)
        {
            base.SetValue(parameter);

            if (value != null)
                value.Cache(Time.renderedFrameCount);
        }

        public override void Interp(Spline from, Spline to, float t)
        {
            if (from == null || to == null)
            {
                base.Interp(from, to, t);
                return;
            }
            
            int frameCount = Time.renderedFrameCount;
            from.Cache(frameCount);
            to.Cache(frameCount);

            for (int i = 0; i < Spline.k_Precision; i++)
            {
                float a = from.cachedData[i];
                float b = to.cachedData[i];
                value.cachedData[i] = a + (b - a) * t;
            }
        }
    }

    public enum TextureParameterDefault
    {
        None,
        Black,
        White,
        Transparent,
        Lut2D
    }

    [Serializable]
    public sealed class TextureParameter : ParameterOverride<Texture>
    {
        public TextureParameterDefault defaultState = TextureParameterDefault.Black;

        public override void Interp(Texture from, Texture to, float t)
        {
            // Both are null, do nothing
            if (from == null && to == null)
            {
                value = null;
                return;
            }

            // Both aren't null we're ready to blend
            if (from != null && to != null)
            {
                value = TextureLerper.instance.Lerp(from, to, t);
                return;
            }

            // One of them is null, blend to/from a default value is applicable
            {

                if (defaultState == TextureParameterDefault.Lut2D)
                {
                    int size = from != null ? from.height : to.height;
                    Texture defaultTexture = RuntimeUtilities.GetLutStrip(size);
                    
                    if (from == null) from = defaultTexture;
                    if (to == null) to = defaultTexture;
                }

                Color tgtColor;
                                
                switch (defaultState)
                {
                    case TextureParameterDefault.Black:
                        tgtColor = Color.black;
                        break;
                    case TextureParameterDefault.White:
                        tgtColor = Color.white;
                        break;
                    case TextureParameterDefault.Transparent:
                        tgtColor = Color.clear;
                        break;
                    case TextureParameterDefault.Lut2D:
                    {
                        // Find the current lut size
                        int size = from != null ? from.height : to.height;
                        Texture defaultTexture = RuntimeUtilities.GetLutStrip(size);
                        if (from == null) from = defaultTexture;
                        if (to == null) to = defaultTexture;

                        value = TextureLerper.instance.Lerp(from, to, t);
                        // All done, return
                        return;
                    }
                    default:
                        // defaultState is none, so just interpolate the base and return
                        base.Interp(from, to, t);
                        return;
                }
                // If we made it this far, tgtColor contains the color we'll be lerping into (or out of)
                if (from == null)
                {
                    // color -> texture lerp, invert ratio
                    value = TextureLerper.instance.Lerp(to, tgtColor, 1f - t);
                }
                else
                {
                    value = TextureLerper.instance.Lerp(from, tgtColor, t);
                }
            }
        }
    }
}
