using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// The base abstract class for all parameter override types.
    /// </summary>
    /// <seealso cref="ParameterOverride{T}"/>
    public abstract class ParameterOverride
    {
        /// <summary>
        /// The override state of this parameter.
        /// </summary>
        public bool overrideState;

        internal abstract void Interp(ParameterOverride from, ParameterOverride to, float t);

        /// <summary>
        /// Returns the computed hash code for this parameter.
        /// </summary>
        /// <returns>A computed hash code</returns>
        public abstract int GetHash();

        /// <summary>
        /// Casts and returns the value stored in this parameter.
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <returns>The value stored in this parameter</returns>
        public T GetValue<T>()
        {
            return ((ParameterOverride<T>)this).value;
        }

        /// <summary>
        /// This method is called right after the parent <see cref="PostProcessEffectSettings"/> has
        /// been initialized. This is used in case you need to access fields or properties that
        /// can't be accessed in the constructor of a <see cref="ScriptableObject"/>
        /// (<c>ParameterOverride</c> objects are generally declared and initialized in a
        /// <see cref="PostProcessEffectSettings"/>).
        /// </summary>
        /// <seealso cref="OnDisable"/>
        protected internal virtual void OnEnable()
        {
        }
        
        /// <summary>
        /// This method is called right before the parent <see cref="PostProcessEffectSettings"/>
        /// gets de-initialized.
        /// </summary>
        /// <seealso cref="OnEnable"/>
        protected internal virtual void OnDisable()
        {
        }

        internal abstract void SetValue(ParameterOverride parameter);
    }

    /// <summary>
    /// The base typed class for all parameter override types.
    /// </summary>
    /// <typeparam name="T">The type of value to store in this <c>ParameterOverride</c></typeparam>
    /// <remarks>
    /// Due to limitations with the serialization system in Unity you shouldn't use this class
    /// directly. Use one of the pre-flatten types (like <see cref="FloatParameter"/> or make your
    /// own by extending this class.
    /// </remarks>
    /// <example>
    /// This sample code shows how to make a custom parameter holding a <c>float</c>.
    /// <code>
    /// [Serializable]
    /// public sealed class FloatParameter : ParameterOverride&lt;float&gt;
    /// {
    ///     public override void Interp(float from, float to, float t)
    ///     {
    ///         value = from + (to - from) * t;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class ParameterOverride<T> : ParameterOverride
    {
        /// <summary>
        /// The value stored in this parameter.
        /// </summary>
        public T value;

        /// <summary>
        /// Creates a <c>ParameterOverride</c> with a default <see cref="value"/> and
        /// <see cref="ParameterOverride.overrideState"/> set to <c>false</c>.
        /// </summary>
        public ParameterOverride()
            : this(default(T), false)
        {
        }

        /// <summary>
        /// Creates a <c>ParameterOverride</c> with a given value and
        /// <see cref="ParameterOverride.overrideState"/> set to <c>false</c>.
        /// </summary>
        /// <param name="value">The value to set this parameter to</param>
        public ParameterOverride(T value)
            : this(value, false)
        {
        }

        /// <summary>
        /// Creates a <c>ParameterOverride</c> with a given value and override state.
        /// </summary>
        /// <param name="value">The value to set this parameter to</param>
        /// <param name="overrideState">The override state for this value</param>
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

        /// <summary>
        /// Interpolates between two values given an interpolation factor <paramref name="t"/>.
        /// </summary>
        /// <param name="from">The value to interpolate from</param>
        /// <param name="to">The value to interpolate to</param>
        /// <param name="t">An interpolation factor (generally in range <c>[0,1]</c>)</param>
        /// <remarks>
        /// By default this method does a "snap" interpolation, meaning it will return the value
        /// <paramref name="to"/> if <paramref name="t"/> is higher than 0, <paramref name="from"/>
        /// otherwise.
        /// </remarks>
        public virtual void Interp(T from, T to, float t)
        {
            // Returns `to` if `dt > 0` by default so we don't have to write overrides for bools and
            // enumerations.
            value = t > 0f ? to : from;
        }

        /// <summary>
        /// Sets the value for this parameter to <paramref name="x"/> and mark the override state
        /// to <c>true</c>.
        /// </summary>
        /// <param name="x"></param>
        public void Override(T x)
        {
            overrideState = true;
            value = x;
        }

        internal override void SetValue(ParameterOverride parameter)
        {
            value = parameter.GetValue<T>();
        }
        
        /// <inheritdoc />
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

        /// <summary>
        /// Implicit conversion between <see cref="ParameterOverride{T}"/> and its value type.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator T(ParameterOverride<T> prop)
        {
            return prop.value;
        }
    }

    // Bypassing the limited unity serialization system...

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <c>float</c> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>.
    /// </remarks>
    [Serializable]
    public sealed class FloatParameter : ParameterOverride<float>
    {
        /// <inheritdoc />
        public override void Interp(float from, float to, float t)
        {
            value = from + (to - from) * t;
        }
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <c>int</c> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>
    /// casted to <c>int</c>.
    /// </remarks>
    [Serializable]
    public sealed class IntParameter : ParameterOverride<int>
    {
        /// <inheritdoc />
        public override void Interp(int from, int to, float t)
        {
            // Int snapping interpolation. Don't use this for enums as they don't necessarily have
            // contiguous values. Use the default interpolator instead (same as bool).
            value = (int)(from + (to - from) * t);
        }
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <c>bool</c> value.
    /// </summary>
    [Serializable]
    public sealed class BoolParameter : ParameterOverride<bool> {}

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <see cref="Color"/> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>
    /// for each channel.
    /// </remarks>
    [Serializable]
    public sealed class ColorParameter : ParameterOverride<Color>
    {
        /// <inheritdoc />
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

        /// <summary>
        /// Implicit conversion between <see cref="ColorParameter"/> and a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector4(ColorParameter prop)
        {
            return prop.value;
        }
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <see cref="Vector2"/> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>
    /// for each axis.
    /// </remarks>
    [Serializable]
    public sealed class Vector2Parameter : ParameterOverride<Vector2>
    {
        /// <inheritdoc />
        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
        }

        /// <summary>
        /// Implicit conversion between <see cref="Vector2Parameter"/> and a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector3(Vector2Parameter prop)
        {
            return prop.value;
        }

        /// <summary>
        /// Implicit conversion between <see cref="Vector2Parameter"/> and a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector4(Vector2Parameter prop)
        {
            return prop.value;
        }
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <see cref="Vector3"/> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>
    /// for each axis.
    /// </remarks>
    [Serializable]
    public sealed class Vector3Parameter : ParameterOverride<Vector3>
    {
        /// <inheritdoc />
        public override void Interp(Vector3 from, Vector3 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
            value.z = from.z + (to.z - from.z) * t;
        }

        /// <summary>
        /// Implicit conversion between <see cref="Vector3Parameter"/> and a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector2(Vector3Parameter prop)
        {
            return prop.value;
        }

        /// <summary>
        /// Implicit conversion between <see cref="Vector3Parameter"/> and a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector4(Vector3Parameter prop)
        {
            return prop.value;
        }
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <see cref="Vector4"/> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>
    /// for each axis.
    /// </remarks>
    [Serializable]
    public sealed class Vector4Parameter : ParameterOverride<Vector4>
    {
        /// <inheritdoc />
        public override void Interp(Vector4 from, Vector4 to, float t)
        {
            value.x = from.x + (to.x - from.x) * t;
            value.y = from.y + (to.y - from.y) * t;
            value.z = from.z + (to.z - from.z) * t;
            value.w = from.w + (to.w - from.w) * t;
        }

        /// <summary>
        /// Implicit conversion between <see cref="Vector4Parameter"/> and a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector2(Vector4Parameter prop)
        {
            return prop.value;
        }

        /// <summary>
        /// Implicit conversion between <see cref="Vector4Parameter"/> and a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="prop">The parameter to implicitly cast</param>
        public static implicit operator Vector3(Vector4Parameter prop)
        {
            return prop.value;
        }
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <see cref="Spline"/> value.
    /// </summary>
    /// <remarks>
    /// The interpolation method for this parameter is the same as <see cref="Mathf.LerpUnclamped"/>
    /// for each point on the curve.
    /// </remarks>
    [Serializable]
    public sealed class SplineParameter : ParameterOverride<Spline>
    {
        /// <inheritdoc />
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
        
        /// <inheritdoc />
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

    /// <summary>
    /// A set of default textures to use as default values for <see cref="TextureParameter"/>.
    /// </summary>
    public enum TextureParameterDefault
    {
        /// <summary>
        /// No texture, or <c>null</c>.
        /// </summary>
        None,

        /// <summary>
        /// A black texture.
        /// </summary>
        Black,

        /// <summary>
        /// A white texture.
        /// </summary>
        White,

        /// <summary>
        /// A transparent texture.
        /// </summary>
        Transparent,

        /// <summary>
        /// A 2D lookup table in strip format with <c>width = height * height</c>.
        /// </summary>
        Lut2D
    }

    /// <summary>
    /// A <see cref="ParameterOverride{T}"/> that holds a <see cref="Texture"/> value.
    /// </summary>
    /// <remarks>
    /// Texture interpolation is done using a classic linear interpolation method.
    /// </remarks>
    [Serializable]
    public sealed class TextureParameter : ParameterOverride<Texture>
    {
        public TextureParameterDefault defaultState = TextureParameterDefault.Black;
        
        /// <inheritdoc />
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

                        // Fail safe in case the lut size is incorrect
                        if (from.width != to.width || from.height != to.height)
                        {
                            value = null;
                            return;
                        }

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
