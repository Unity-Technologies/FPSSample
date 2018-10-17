using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    public sealed class PostProcessProfile : ScriptableObject
    {
        [Tooltip("A list of all settings & overrides.")]
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

        public void Reset()
        {
            isDirty = true;
        }

        public T AddSettings<T>()
            where T : PostProcessEffectSettings
        {
            return (T)AddSettings(typeof(T));
        }

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

        public PostProcessEffectSettings AddSettings(PostProcessEffectSettings effect)
        {
            if (HasSettings(settings.GetType()))
                throw new InvalidOperationException("Effect already exists in the stack");

            settings.Add(effect);
            isDirty = true;
            return effect;
        }

        public void RemoveSettings<T>()
            where T : PostProcessEffectSettings
        {
            RemoveSettings(typeof(T));
        }

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
                throw new InvalidOperationException("Effect doesn't exist in the stack");

            settings.RemoveAt(toRemove);
            isDirty = true;
        }

        public bool HasSettings<T>()
            where T : PostProcessEffectSettings
        {
            return HasSettings(typeof(T));
        }

        public bool HasSettings(Type type)
        {
            foreach (var setting in settings)
            {
                if (setting.GetType() == type)
                    return true;
            }

            return false;
        }

        public T GetSetting<T>() where T : PostProcessEffectSettings
        {
            foreach (var setting in settings)
            {
                if (setting is T)
                    return setting as T;
            }
            return null;
        }

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
