using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    public sealed class PostProcessBundle
    {
        public PostProcessAttribute attribute { get; private set; }
        public PostProcessEffectSettings settings { get; private set; }

        internal PostProcessEffectRenderer renderer
        {
            get
            {
                if (m_Renderer == null)
                {
                    Assert.IsNotNull(attribute.renderer);
                    var rendererType = attribute.renderer;
                    m_Renderer = (PostProcessEffectRenderer)Activator.CreateInstance(rendererType);
                    m_Renderer.SetSettings(settings);
                    m_Renderer.Init();
                }

                return m_Renderer;
            }
        }

        PostProcessEffectRenderer m_Renderer;

        internal PostProcessBundle(PostProcessEffectSettings settings)
        {
            // If settings is null, it means that at some point a null element has been added to
            // the volume effect list or there was a deserialization error and a reference to
            // the settings scriptableobject was lost
            Assert.IsNotNull(settings);
            this.settings = settings;
            attribute = settings.GetType().GetAttribute<PostProcessAttribute>();
        }

        internal void Release()
        {
            if (m_Renderer != null)
                m_Renderer.Release();

            RuntimeUtilities.Destroy(settings);
        }

        internal void ResetHistory()
        {
            if (m_Renderer != null)
                m_Renderer.ResetHistory();
        }

        internal T CastSettings<T>()
            where T : PostProcessEffectSettings
        {
            return (T)settings;
        }

        internal T CastRenderer<T>()
            where T : PostProcessEffectRenderer
        {
            return (T)renderer;
        }
    }
}
