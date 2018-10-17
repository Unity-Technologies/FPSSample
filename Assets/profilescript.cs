using UnityEngine;
using UnityEngine.Profiling;

[ExecuteInEditMode]
public class profilescript : MonoBehaviour
{
    private double render_avg = 0.0;
    private double render_shadow_casters_local = 0.0;
    private double render_shadow_casters_detail_point = 0.0;
    private double render_cull_per_object_lights = 0.0;
    private double render_node_queue = 0.0;
    private double render_prepare_lights_for_gpu = 0.0;

    void OnGUI()
    {
        render_avg = render_avg * 0.99 + Recorder.Get("Camera.Render").elapsedNanoseconds * 0.01;
        render_shadow_casters_local = render_shadow_casters_local * 0.99 + Recorder.Get("Shadows.CullShadowCastersLocal").elapsedNanoseconds * 0.01;
        render_shadow_casters_detail_point = render_shadow_casters_detail_point * 0.99 + Recorder.Get("Shadows.CullShadowCastersDetailPoint").elapsedNanoseconds * 0.01;
        render_cull_per_object_lights = render_cull_per_object_lights * 0.99 + Recorder.Get("CullPerObjectLights").elapsedNanoseconds * 0.01;
        render_node_queue = render_node_queue * 0.99 + Recorder.Get("ExtractRenderNodeQueue").elapsedNanoseconds * 0.01;
        render_prepare_lights_for_gpu = render_prepare_lights_for_gpu * 0.99 + Recorder.Get("PrepareLightsForGPU").elapsedNanoseconds * 0.01;

        GUILayout.Label(string.Format("Camera.Render: {0}", render_avg * 0.000001));
        GUILayout.Label(string.Format("Shadows.CullShadowCastersLocal: {0}", render_shadow_casters_local * 0.000001));
        GUILayout.Label(string.Format("Shadows.CullShadowCastersDetailPoint: {0}", render_shadow_casters_detail_point * 0.000001));
        GUILayout.Label(string.Format("CullPerObjectLights: {0}", render_cull_per_object_lights * 0.000001));
        GUILayout.Label(string.Format("ExtractRenderNodeQueue: {0}", render_node_queue * 0.000001));
        GUILayout.Label(string.Format("PrepareLightsForGPU: {0}", render_prepare_lights_for_gpu * 0.000001));
    }
}