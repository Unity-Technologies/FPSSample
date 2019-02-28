using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// The base class for all post-processing effect settings. Any <see cref="ParameterOverride"/>
    /// members found in this class will be automatically handled and interpolated by the volume
    /// framework.
    /// </summary>
    /// <example>
    /// <code>
    /// [Serializable]
    /// [PostProcess(typeof(ExampleRenderer), "Custom/ExampleEffect")]
    /// public sealed class ExampleEffect : PostProcessEffectSettings
    /// {
    ///     [Range(0f, 1f), Tooltip("Effect intensity.")]
    ///     public FloatParameter intensity = new FloatParameter { value = 0f };
    ///
    ///     public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    ///     {
    ///         return enabled.value
    ///             &amp;&amp; intensity.value > 0f; // Only render the effect if intensity is greater than 0
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class PostProcessEffectSettings : ScriptableObject
    {
        /// <summary>
        /// The active state of the set of parameter defined in this class.
        /// </summary>
        /// <seealso cref="enabled"/>
        public bool active = true;

        /// <summary>
        /// The true state of the effect override in the stack. Setting this to <c>false</c> will
        /// disable rendering for this effect assuming a volume with a higher priority doesn't
        /// override it to <c>true</c>.
        /// </summary>
        public BoolParameter enabled = new BoolParameter { overrideState = true, value = false };

        internal ReadOnlyCollection<ParameterOverride> parameters;

        void OnEnable()
        {
            // Automatically grab all fields of type ParameterOverride for this instance
            parameters = GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => t.FieldType.IsSubclassOf(typeof(ParameterOverride)))
                .OrderBy(t => t.MetadataToken) // Guaranteed order
                .Select(t => (ParameterOverride)t.GetValue(this))
                .ToList()
                .AsReadOnly();

            foreach (var parameter in parameters)
                parameter.OnEnable();
        }

        void OnDisable()
        {
            if (parameters == null)
                return;

            foreach (var parameter in parameters)
                parameter.OnDisable();
        }

        /// <summary>
        /// Sets all the overrides for this effect to a given value.
        /// </summary>
        /// <param name="state">The value to set the override states to</param>
        /// <param name="excludeEnabled">If <c>false</c>, the <see cref="enabled"/> field will also
        /// be set to the given <see cref="state"/> value.</param>
        public void SetAllOverridesTo(bool state, bool excludeEnabled = true)
        {
            foreach (var prop in parameters)
            {
                if (excludeEnabled && prop == enabled)
                    continue;

                prop.overrideState = state;
            }
        }
        
        /// <summary>
        /// Returns <c>true</c> if the effect is currently enabled and supported.
        /// </summary>
        /// <param name="context">The current post-processing render context</param>
        /// <returns><c>true</c> if the effect is currently enabled and supported</returns>
        public virtual bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value;
        }

        /// <summary>
        /// Returns the computed hash code for this parameter.
        /// </summary>
        /// <returns>A computed hash code</returns>
        public int GetHash()
        {
            // Custom hashing function used to compare the state of settings (it's not meant to be
            // unique but to be a quick way to check if two setting sets have the same state or not).
            // Hash collision rate should be pretty low.
            unchecked
            {
                //return parameters.Aggregate(17, (i, p) => i * 23 + p.GetHash());

                int hash = 17;

                foreach (var p in parameters)
                    hash = hash * 23 + p.GetHash();

                return hash;
            }
        }
    }
}
