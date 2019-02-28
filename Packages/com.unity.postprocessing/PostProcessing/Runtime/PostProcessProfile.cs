using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// An asset holding a set of post-processing settings to use with a <see cref="PostProcessVolume"/>.
    /// </summary>
    /// <seealso cref="PostProcessVolume"/>
    public sealed class PostProcessProfile : ScriptableObject
    {
        /// <summary>
        /// A list of all settings stored in this profile.
        /// </summary>
        [Tooltip("A list of all settings currently stored in this profile.")]
        public List<PostProcessEffectSettings> settings = new List<PostProcessEffectSettings>();

        // Editor only, doesn't have any use outside of it
        [NonSerialized]
        public bool isDirty = true;

        void OnEnable()
        {
            // Make sure every setting is valid. If a profile holds a script that doesn't exist
            // anymore, nuke it to keep the profile clean. Note that if you delete a script that is
            // currently in use in a profile you'll still get a one-time error in the console, it's
            // harmless and happens because Unity does a redraw of the editor (and thus the current
            // frame) before the recompilation step.
            settings.RemoveAll(x => x == null);
        }

        /// <summary>
        /// Adds settings for an effect to the profile.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="PostProcessEffectSettings"/></typeparam>
        /// <returns>The instance created from the given type</returns>
        /// <seealso cref="PostProcessEffectSettings"/>
        public T AddSettings<T>()
            where T : PostProcessEffectSettings
        {
            return (T)AddSettings(typeof(T));
        }

        /// <summary>
        /// Adds settings for an effect to the profile.
        /// </summary>
        /// <param name="type">A type of <see cref="PostProcessEffectSettings"/></param>
        /// <returns>The instance created from the given type</returns>
        /// <seealso cref="PostProcessEffectSettings"/>
        public PostProcessEffectSettings AddSettings(Type type)
        {
            if (HasSettings(type))
                throw new InvalidOperationException("Effect already exists in the stack");

            var effect = (PostProcessEffectSettings)CreateInstance(type);
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            effect.name = type.Name;
            effect.enabled.value = true;
            settings.Add(effect);
            isDirty = true;
            return effect;
        }

        /// <summary>
        /// Adds settings for an effect to the profile.
        /// </summary>
        /// <param name="effect">An instance of <see cref="PostProcessEffectSettings"/></param>
        /// <returns>The given effect instance</returns>
        /// <seealso cref="PostProcessEffectSettings"/>
        public PostProcessEffectSettings AddSettings(PostProcessEffectSettings effect)
        {
            if (HasSettings(settings.GetType()))
                throw new InvalidOperationException("Effect already exists in the stack");

            settings.Add(effect);
            isDirty = true;
            return effect;
        }

        /// <summary>
        /// Removes settings for an effect from the profile.
        /// </summary>
        /// <typeparam name="T">The type to look for and remove from the profile</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if the effect doesn't exist in the
        /// profile</exception>
        public void RemoveSettings<T>()
            where T : PostProcessEffectSettings
        {
            RemoveSettings(typeof(T));
        }

        /// <summary>
        /// Removes settings for an effect from the profile.
        /// </summary>
        /// <param name="type">The type to look for and remove from the profile</param>
        /// <exception cref="InvalidOperationException">Thrown if the effect doesn't exist in the
        /// profile</exception>
        public void RemoveSettings(Type type)
        {
            int toRemove = -1;

            for (int i = 0; i < settings.Count; i++)
            {
                if (settings[i].GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove < 0)
                throw new InvalidOperationException("Effect doesn't exist in the profile");

            settings.RemoveAt(toRemove);
            isDirty = true;
        }

        /// <summary>
        /// Checks if an effect has been added to the profile.
        /// </summary>
        /// <typeparam name="T">The type to look for</typeparam>
        /// <returns><c>true</c> if the effect exists in the profile, <c>false</c> otherwise</returns>
        public bool HasSettings<T>()
            where T : PostProcessEffectSettings
        {
            return HasSettings(typeof(T));
        }

        /// <summary>
        /// Checks if an effect has been added to the profile.
        /// </summary>
        /// <param name="type">The type to look for</param>
        /// <returns><c>true</c> if the effect exists in the profile, <c>false</c> otherwise</returns>
        public bool HasSettings(Type type)
        {
            foreach (var setting in settings)
            {
                if (setting.GetType() == type)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns settings for a given effect type.
        /// </summary>
        /// <typeparam name="T">The type to look for</typeparam>
        /// <returns>Settings for the given effect type, <c>null</c> otherwise</returns>
        public T GetSetting<T>() where T : PostProcessEffectSettings
        {
            foreach (var setting in settings)
            {
                if (setting is T)
                    return setting as T;
            }

            return null;
        }

        /// <summary>
        /// Gets settings for a given effect type.
        /// </summary>
        /// <typeparam name="T">The type to look for</typeparam>
        /// <param name="outSetting">When this method returns, contains the value associated with
        /// the specified type, if the type is found; otherwise, this parameter will be <c>null</c>.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the effect exists in the profile, <c>false</c> otherwise</returns>
        public bool TryGetSettings<T>(out T outSetting)
            where T : PostProcessEffectSettings
        {
            var type = typeof(T);
            outSetting = null;

            foreach (var setting in settings)
            {
                if (setting.GetType() == type)
                {
                    outSetting = (T)setting;
                    return true;
                }
            }

            return false;
        }
    }
}
