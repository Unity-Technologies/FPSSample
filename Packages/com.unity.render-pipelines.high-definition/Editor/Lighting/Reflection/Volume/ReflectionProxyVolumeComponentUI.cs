using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<ReflectionProxyVolumeComponentUI, SerializedReflectionProxyVolumeComponent>;

    class ReflectionProxyVolumeComponentUI : BaseUI<SerializedReflectionProxyVolumeComponent>
    {
#pragma warning disable 618 //CED
        public static readonly CED.IDrawer Inspector;
#pragma warning restore 618

        static ReflectionProxyVolumeComponentUI()
        {
            Inspector = CED.Select(
                    (s, d, o) => s.proxyVolume,
                    (s, d, o) => d.proxyVolume,
                    ProxyVolumeUI.SectionShape
                    );
        }

        public ProxyVolumeUI proxyVolume = new ProxyVolumeUI();

        public ReflectionProxyVolumeComponentUI()
            : base(0)
        {
        }

        public override void Reset(SerializedReflectionProxyVolumeComponent data, UnityAction repaint)
        {
            if (data != null)
                proxyVolume.Reset(data.proxyVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            proxyVolume.Update();
            base.Update();
        }

        public static void DrawHandles_EditBase(ReflectionProxyVolumeComponentUI ui, ReflectionProxyVolumeComponent target)
        {
            ProxyVolumeUI.DrawHandles_EditBase(target.transform, target.proxyVolume, ui.proxyVolume, target);
        }

        public static void DrawGizmos_EditNone(ReflectionProxyVolumeComponent target)
        {
            ProxyVolumeUI.DrawGizmos(target.transform, target.proxyVolume);
        }
    }
}
