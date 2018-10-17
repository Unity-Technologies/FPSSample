using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [Decorator(typeof(TrackballAttribute))]
    public sealed class TrackballDecorator : AttributeDecorator
    {
        static readonly int k_ThumbHash = "colorWheelThumb".GetHashCode();
        static Material s_Material;

        bool m_ResetState;
        Vector2 m_CursorPos;

        public override bool IsAutoProperty()
        {
            return false;
        }

        public override bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute)
        {
            if (property.propertyType != SerializedPropertyType.Vector4)
                return false;

            var value = property.vector4Value;

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUI.DisabledScope(!overrideState.boolValue))
                    DrawWheel(ref value, overrideState.boolValue, (TrackballAttribute)attribute);

                DrawLabelAndOverride(title, overrideState);
            }

            if (m_ResetState)
            {
                value = Vector4.zero;
                m_ResetState = false;
            }

            property.vector4Value = value;

            return true;
        }

        void DrawWheel(ref Vector4 value, bool overrideState, TrackballAttribute attr)
        {
            var wheelRect = GUILayoutUtility.GetAspectRect(1f);
            float size = wheelRect.width;
            float hsize = size / 2f;
            float radius = 0.38f * size;

            Vector3 hsv;
            Color.RGBToHSV(value, out hsv.x, out hsv.y, out hsv.z);
            float offset = value.w;

            // Thumb
            var thumbPos = Vector2.zero;
            float theta = hsv.x * (Mathf.PI * 2f);
            thumbPos.x = Mathf.Cos(theta + (Mathf.PI / 2f));
            thumbPos.y = Mathf.Sin(theta - (Mathf.PI / 2f));
            thumbPos *= hsv.y * radius;

            // Draw the wheel
            if (Event.current.type == EventType.Repaint)
            {
                // Retina support
                float scale = EditorGUIUtility.pixelsPerPoint;

                if (s_Material == null)
                    s_Material = new Material(Shader.Find("Hidden/PostProcessing/Editor/Trackball")) { hideFlags = HideFlags.HideAndDontSave };

                // Wheel texture
            #if UNITY_2018_1_OR_NEWER
                const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.sRGB;
            #else
                const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.Linear;
            #endif

                var oldRT = RenderTexture.active;
                var rt = RenderTexture.GetTemporary((int)(size * scale), (int)(size * scale), 0, RenderTextureFormat.ARGB32, kReadWrite);
                s_Material.SetFloat("_Offset", offset);
                s_Material.SetFloat("_DisabledState", overrideState ? 1f : 0.5f);
                s_Material.SetVector("_Resolution", new Vector2(size * scale, size * scale / 2f));
                Graphics.Blit(null, rt, s_Material, EditorGUIUtility.isProSkin ? 0 : 1);
                RenderTexture.active = oldRT;

                GUI.DrawTexture(wheelRect, rt);
                RenderTexture.ReleaseTemporary(rt);

                var thumbSize = Styling.wheelThumbSize;
                var thumbSizeH = thumbSize / 2f;
                Styling.wheelThumb.Draw(new Rect(wheelRect.x + hsize + thumbPos.x - thumbSizeH.x, wheelRect.y + hsize + thumbPos.y - thumbSizeH.y, thumbSize.x, thumbSize.y), false, false, false, false);
            }

            // Input
            var bounds = wheelRect;
            bounds.x += hsize - radius;
            bounds.y += hsize - radius;
            bounds.width = bounds.height = radius * 2f;
            hsv = GetInput(bounds, hsv, thumbPos, radius);
            value = Color.HSVToRGB(hsv.x, hsv.y, 1f);
            value.w = offset;

            // Offset
            var sliderRect = GUILayoutUtility.GetRect(1f, 17f);
            float padding = sliderRect.width * 0.05f; // 5% padding
            sliderRect.xMin += padding;
            sliderRect.xMax -= padding;
            value.w = GUI.HorizontalSlider(sliderRect, value.w, -1f, 1f);

            if (attr.mode == TrackballAttribute.Mode.None)
                return;

            // Values
            var displayValue = Vector3.zero;

            switch (attr.mode)
            {
                case TrackballAttribute.Mode.Lift: displayValue = ColorUtilities.ColorToLift(value);
                    break;
                case TrackballAttribute.Mode.Gamma: displayValue = ColorUtilities.ColorToInverseGamma(value);
                    break;
                case TrackballAttribute.Mode.Gain: displayValue = ColorUtilities.ColorToGain(value);
                    break;
            }

            using (new EditorGUI.DisabledGroupScope(true))
            {
                var valuesRect = GUILayoutUtility.GetRect(1f, 17f);
                valuesRect.width /= 3f;
                GUI.Label(valuesRect, displayValue.x.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
                valuesRect.x += valuesRect.width;
                GUI.Label(valuesRect, displayValue.y.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
                valuesRect.x += valuesRect.width;
                GUI.Label(valuesRect, displayValue.z.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
                valuesRect.x += valuesRect.width;
            }
        }

        void DrawLabelAndOverride(GUIContent title, SerializedProperty overrideState)
        {
            // Title
            var areaRect = GUILayoutUtility.GetRect(1f, 17f);
            var labelSize = Styling.wheelLabel.CalcSize(title);
            var labelRect = new Rect(areaRect.x + areaRect.width / 2 - labelSize.x / 2, areaRect.y, labelSize.x, labelSize.y);
            GUI.Label(labelRect, title, Styling.wheelLabel);

            // Override checkbox
            var overrideRect = new Rect(labelRect.x - 17, labelRect.y + 3, 17f, 17f);
            EditorUtilities.DrawOverrideCheckbox(overrideRect, overrideState);
        }

        Vector3 GetInput(Rect bounds, Vector3 hsv, Vector2 thumbPos, float radius)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(k_ThumbHash, FocusType.Passive, bounds);
            var mousePos = e.mousePosition;

            if (e.type == EventType.MouseDown && GUIUtility.hotControl == 0 && bounds.Contains(mousePos))
            {
                if (e.button == 0)
                {
                    var center = new Vector2(bounds.x + radius, bounds.y + radius);
                    float dist = Vector2.Distance(center, mousePos);

                    if (dist <= radius)
                    {
                        e.Use();
                        m_CursorPos = new Vector2(thumbPos.x + radius, thumbPos.y + radius);
                        GUIUtility.hotControl = id;
                        GUI.changed = true;
                    }
                }
                else if (e.button == 1)
                {
                    e.Use();
                    GUI.changed = true;
                    m_ResetState = true;
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && GUIUtility.hotControl == id)
            {
                e.Use();
                GUI.changed = true;
                m_CursorPos += e.delta * GlobalSettings.trackballSensitivity;
                GetWheelHueSaturation(m_CursorPos.x, m_CursorPos.y, radius, out hsv.x, out hsv.y);
            }
            else if (e.rawType == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == id)
            {
                e.Use();
                GUIUtility.hotControl = 0;
            }

            return hsv;
        }

        void GetWheelHueSaturation(float x, float y, float radius, out float hue, out float saturation)
        {
            float dx = (x - radius) / radius;
            float dy = (y - radius) / radius;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            hue = Mathf.Atan2(dx, -dy);
            hue = 1f - ((hue > 0) ? hue : (Mathf.PI * 2f) + hue) / (Mathf.PI * 2f);
            saturation = Mathf.Clamp01(d);
        }
    }
}
