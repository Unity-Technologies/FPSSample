using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class LaunchEventBehavior : IPushButtonBehavior
    {
        public void OnClicked(string value)
        {
            var allComponent = UnityEngine.Experimental.VFX.VFXManager.GetComponents();
            foreach (var component in allComponent)
            {
                component.SendEvent(value);
            }
        }
    }

    [VFXInfo]
    class VFXBasicEvent : VFXContext
    {
        [VFXSetting, PushButton(typeof(LaunchEventBehavior), "Send"), Delayed]
        public string eventName = "OnPlay";

        public VFXBasicEvent() : base(VFXContextType.kEvent, VFXDataType.kNone, VFXDataType.kSpawnEvent) {}
        public override string name { get { return "Event"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return null;
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
