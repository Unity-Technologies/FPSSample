using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDLightUI
    {
        [MenuItem("CONTEXT/Light/Remove Component", false, 0)]
        static void RemoveLight(MenuCommand menuCommand)
        {
            GameObject go = ((Light)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(go.GetComponent<Light>());
            Undo.DestroyObjectImmediate(go.GetComponent<HDAdditionalLightData>());
            Undo.DestroyObjectImmediate(go.GetComponent<AdditionalShadowData>());
        }

        [MenuItem("CONTEXT/Light/Reset", false, 0)]
        static void ResetLight(MenuCommand menuCommand)
        {
            GameObject go = ((Light)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Light light = go.GetComponent<Light>();
            HDAdditionalLightData lightAdditionalData = go.GetComponent<HDAdditionalLightData>();
            AdditionalShadowData shadowAdditionalData = go.GetComponent<AdditionalShadowData>();

            Assert.IsNotNull(light);
            Assert.IsNotNull(lightAdditionalData);
            Assert.IsNotNull(shadowAdditionalData);

            Undo.RecordObjects(new UnityEngine.Object[] { light, lightAdditionalData, shadowAdditionalData }, "Reset HD Light");
            light.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalLightData, thus why we don't wrap it in a Reset() function
            HDUtils.s_DefaultHDAdditionalLightData.CopyTo(lightAdditionalData);
            HDUtils.s_DefaultAdditionalShadowData.CopyTo(shadowAdditionalData);
        }
    }
}
