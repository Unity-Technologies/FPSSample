using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreLightEditorUtilities
    {
        static Vector2 SliderPlaneHandle(Vector3 origin, Vector3 axis1, Vector3 axis2, Vector2 position)
        {
            Vector3 pos = origin + position.x * axis1 + position.y * axis2;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider2D(pos, Vector3.forward, axis1, axis2, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                position = new Vector2(Vector3.Dot(pos, axis1), Vector3.Dot(pos, axis2));
            }
            GUI.changed |= temp;
            return position;
        }

        static float SliderLineHandle(Vector3 position, Vector3 direction, float value)
        {
            Vector3 pos = position + direction * value;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            pos = Handles.Slider(pos, direction, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                value = Vector3.Dot(pos - position, direction);
            }
            GUI.changed |= temp;
            return value;
        }

        static float SliderCircleHandle(Vector3 position, Vector3 normal, Vector3 zeroValueDirection, float angleValue, float radius)
        {
            zeroValueDirection.Normalize();
            normal.Normalize();
            Quaternion rot = Quaternion.AngleAxis(angleValue, normal);
            Vector3 pos = position + rot * zeroValueDirection * radius;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            Vector3 tangeant = Vector3.Cross(normal, (pos - position).normalized);
            pos = Handles.Slider(pos, tangeant, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
            {
                Vector3 dir = (pos - position).normalized;
                Vector3 cross = Vector3.Cross(zeroValueDirection, dir);
                int sign = ((cross - normal).sqrMagnitude < (-cross - normal).sqrMagnitude) ? 1 : -1;
                angleValue = Mathf.Acos(Vector3.Dot(zeroValueDirection, dir)) * Mathf.Rad2Deg * sign;
            }
            GUI.changed |= temp;
            return angleValue;
        }

        static int s_SliderSpotAngleId;

        static float SizeSliderSpotAngle(Vector3 position, Vector3 forward, Vector3 axis, float range, float spotAngle)
        {
            if (Math.Abs(spotAngle) <= 0.05f && GUIUtility.hotControl != s_SliderSpotAngleId)
                return spotAngle;
            var angledForward = Quaternion.AngleAxis(Mathf.Max(spotAngle, 0.05f) * 0.5f, axis) * forward;
            var centerToLeftOnSphere = (angledForward * range + position) - (position + forward * range);
            bool temp = GUI.changed;
            GUI.changed = false;
            var newMagnitude = Mathf.Max(0f, SliderLineHandle(position + forward * range, centerToLeftOnSphere.normalized, centerToLeftOnSphere.magnitude));
            if (GUI.changed)
            {
                s_SliderSpotAngleId = GUIUtility.hotControl;
                centerToLeftOnSphere = centerToLeftOnSphere.normalized * newMagnitude;
                angledForward = (centerToLeftOnSphere + (position + forward * range) - position).normalized;
                spotAngle = Mathf.Clamp(Mathf.Acos(Vector3.Dot(forward, angledForward)) * Mathf.Rad2Deg * 2, 0f, 179f);
                if (spotAngle <= 0.05f || float.IsNaN(spotAngle))
                    spotAngle = 0f;
            }
            GUI.changed |= temp;
            return spotAngle;
        }

        public static Color GetLightHandleColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = Mathf.Clamp01(color.a * 2);
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        public static Color GetLightBehindObjectWireframeColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = 0.2f;
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        // Don't use Handles.Disc as it break the highlight of the gizmo axis, use our own draw disc function instead for gizmo
        public static void DrawWireDisc(Quaternion q, Vector3 position, Vector3 axis, float radius)
        {
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

            Gizmos.color = Color.white;
            float theta = 0.0f;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = rotation * new Vector3(x, y, 0);
            pos += position;
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < 2.0f * Mathf.PI; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);

                newPos = rotation * new Vector3(x, y, 0);
                newPos += position;
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }

        public static void DrawSpotlightWireframe(Vector3 outerAngleInnerAngleRange, float shadowPlaneDistance = -1f)
        {
            float outerAngle = outerAngleInnerAngleRange.x;
            float innerAngle = outerAngleInnerAngleRange.y;
            float range = outerAngleInnerAngleRange.z;


            var outerDiscRadius = range * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
            var outerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle * 0.5f) * range;
            var vectorLineUp = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.up * outerDiscRadius);
            var vectorLineLeft = Vector3.Normalize(Vector3.forward * outerDiscDistance + Vector3.left * outerDiscRadius);

            if(innerAngle>0f)
            {
                var innerDiscRadius = range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * range;
                DrawConeWireframe(innerDiscRadius, innerDiscDistance);
            }
            DrawConeWireframe(outerDiscRadius, outerDiscDistance);
            Handles.DrawWireArc(Vector3.zero, Vector3.right, vectorLineUp, outerAngle, range);
            Handles.DrawWireArc(Vector3.zero, Vector3.up, vectorLineLeft, outerAngle, range);

            if (shadowPlaneDistance > 0)
            {
                var shadowDiscRadius = shadowPlaneDistance * Mathf.Sin(outerAngle * Mathf.Deg2Rad * 0.5f);
                var shadowDiscDistance = Mathf.Cos(Mathf.Deg2Rad * outerAngle / 2) * shadowPlaneDistance;
                Handles.DrawWireDisc(Vector3.forward * shadowDiscDistance, Vector3.forward, shadowDiscRadius);
            }
        }

        static void DrawConeWireframe(float radius, float height)
        {
            var rangeCenter = Vector3.forward * height;
            var rangeUp = rangeCenter + Vector3.up * radius;
            var rangeDown = rangeCenter - Vector3.up * radius;
            var rangeRight = rangeCenter + Vector3.right * radius;
            var rangeLeft = rangeCenter - Vector3.right * radius;

            //Draw Lines
            Handles.DrawLine(Vector3.zero, rangeUp);
            Handles.DrawLine(Vector3.zero, rangeDown);
            Handles.DrawLine(Vector3.zero, rangeRight);
            Handles.DrawLine(Vector3.zero, rangeLeft);
            
            Handles.DrawWireDisc(Vector3.forward * height, Vector3.forward, radius);
        }

        public static Vector3 DrawSpotlightHandle(Vector3 outerAngleInnerAngleRange)
        {
            float outerAngle = outerAngleInnerAngleRange.x;
            float innerAngle = outerAngleInnerAngleRange.y;
            float range = outerAngleInnerAngleRange.z;

            if (innerAngle > 0f)
            {
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.right, range, innerAngle);
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.left, range, innerAngle);
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.up, range, innerAngle);
                innerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.down, range, innerAngle);
            }

            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.right, range, outerAngle);
            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.left, range, outerAngle);
            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.up, range, outerAngle);
            outerAngle = SizeSliderSpotAngle(Vector3.zero, Vector3.forward, Vector3.down, range, outerAngle);

            range = SliderLineHandle(Vector3.zero, Vector3.forward, range);

            return new Vector3(outerAngle, innerAngle, range);
        }
        
        public static void DrawAreaLightWireframe(Vector2 rectangleSize)
        {
            Handles.DrawWireCube(Vector3.zero, rectangleSize);
        }

        public static Vector2 DrawAreaLightHandle(Vector2 rectangleSize, bool withYAxis)
        {
            float halfWidth = rectangleSize.x * 0.5f;
            float halfHeight = rectangleSize.y * 0.5f;

            EditorGUI.BeginChangeCheck();
            halfWidth = SliderLineHandle(Vector3.zero, Vector3.right, halfWidth);
            halfWidth = SliderLineHandle(Vector3.zero, Vector3.left, halfWidth);
            if (EditorGUI.EndChangeCheck())
            {
                halfWidth = Mathf.Max(0f, halfWidth);
            }

            if (withYAxis)
            {
                EditorGUI.BeginChangeCheck();
                halfHeight = SliderLineHandle(Vector3.zero, Vector3.up, halfHeight);
                halfHeight = SliderLineHandle(Vector3.zero, Vector3.down, halfHeight);
                if (EditorGUI.EndChangeCheck())
                {
                    halfHeight = Mathf.Max(0f, halfHeight);
                }
            }

            return new Vector2(halfWidth * 2f, halfHeight * 2f);
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static void DrawPyramidFrustumWireframe(Vector4 aspectFovMaxRangeMinRange, float distanceTruncPlane = 0f)
        {
            float aspect = aspectFovMaxRangeMinRange.x;
            float fov = aspectFovMaxRangeMinRange.y;
            float maxRange = aspectFovMaxRangeMinRange.z;
            float minRange = aspectFovMaxRangeMinRange.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
            
            var startAngles = new Vector3[4];
            if (minRange > 0.0f)
            {
                startAngles = GetProjectedRectAngles(minRange, aspect, tanfov);
                Handles.DrawLine(startAngles[0], startAngles[1]);
                Handles.DrawLine(startAngles[1], startAngles[2]);
                Handles.DrawLine(startAngles[2], startAngles[3]);
                Handles.DrawLine(startAngles[3], startAngles[0]);
            }

            if (distanceTruncPlane > 0f)
            {
                var truncAngles = GetProjectedRectAngles(distanceTruncPlane, aspect, tanfov);
                Handles.DrawLine(truncAngles[0], truncAngles[1]);
                Handles.DrawLine(truncAngles[1], truncAngles[2]);
                Handles.DrawLine(truncAngles[2], truncAngles[3]);
                Handles.DrawLine(truncAngles[3], truncAngles[0]);
            }

            var endAngles = GetProjectedRectAngles(maxRange, aspect, tanfov);
            Handles.DrawLine(endAngles[0], endAngles[1]);
            Handles.DrawLine(endAngles[1], endAngles[2]);
            Handles.DrawLine(endAngles[2], endAngles[3]);
            Handles.DrawLine(endAngles[3], endAngles[0]);

            Handles.DrawLine(startAngles[0], endAngles[0]);
            Handles.DrawLine(startAngles[1], endAngles[1]);
            Handles.DrawLine(startAngles[2], endAngles[2]);
            Handles.DrawLine(startAngles[3], endAngles[3]);
        }

        static Vector3[] GetProjectedRectAngles(float distance, float aspect, float tanFOV)
        {
            Vector3 sizeX;
            Vector3 sizeY;
            float minXYTruncSize = distance * tanFOV;
            if (aspect >= 1.0f)
            {
                sizeX = new Vector3(minXYTruncSize * aspect, 0, 0);
                sizeY = new Vector3(0, minXYTruncSize, 0);
            }
            else
            {
                sizeX = new Vector3(minXYTruncSize, 0, 0);
                sizeY = new Vector3(0, minXYTruncSize / aspect, 0);
            }
            Vector3 center = new Vector3(0, 0, distance);
            Vector3[] angles =
            {
                center + sizeX + sizeY,
                center - sizeX + sizeY,
                center - sizeX - sizeY,
                center + sizeX - sizeY
            };

            return angles;
        }

        public static Vector4 DrawPyramidFrustumHandle(Vector4 aspectFovMaxRangeMinRange, bool useNearPlane, float minAspect = 0.05f, float maxAspect = 20f, float minFov = 1f)
        {
            float aspect = aspectFovMaxRangeMinRange.x;
            float fov = aspectFovMaxRangeMinRange.y;
            float maxRange = aspectFovMaxRangeMinRange.z;
            float minRange = aspectFovMaxRangeMinRange.w;
            float tanfov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);

            var e = GetProjectedRectAngles(maxRange, aspect, tanfov);
            
            if (useNearPlane)
            {
                minRange = SliderLineHandle(Vector3.zero, Vector3.forward, minRange);
            }

            maxRange = SliderLineHandle(Vector3.zero, Vector3.forward, maxRange);
            
            float distanceRight = HandleUtility.DistanceToLine(e[0], e[3]);
            float distanceLeft = HandleUtility.DistanceToLine(e[1], e[2]);
            float distanceUp = HandleUtility.DistanceToLine(e[0], e[1]);
            float distanceDown = HandleUtility.DistanceToLine(e[2], e[3]);

            int pointIndex = 0;
            if (distanceRight < distanceLeft)
            {
                if (distanceUp < distanceDown)
                    pointIndex = 0;
                else
                    pointIndex = 3;
            }
            else
            {
                if (distanceUp < distanceDown)
                    pointIndex = 1;
                else
                    pointIndex = 2;
            }
            
            Vector2 send = e[pointIndex];
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            EditorGUI.BeginChangeCheck();
            Vector2 received = SliderPlaneHandle(farEnd, Vector3.right, Vector3.up, send);
            if (EditorGUI.EndChangeCheck())
            {
                bool fixedFov = Event.current.control && !Event.current.shift;
                bool fixedAspect = Event.current.shift && !Event.current.control;

                //work on positive quadrant
                int xSign = send.x < 0f ? -1 : 1;
                int ySign = send.y < 0f ? -1 : 1;
                Vector2 corrected = new Vector2(received.x * xSign, received.y * ySign);

                //fixed aspect correction
                if (fixedAspect)
                {
                    corrected.x = corrected.y * aspect;
                }

                //remove aspect deadzone
                if (corrected.x > maxAspect * corrected.y)
                {
                    corrected.y = corrected.x * minAspect;
                }
                if (corrected.x < minAspect * corrected.y)
                {
                    corrected.x = corrected.y / maxAspect;
                }

                //remove fov deadzone
                float deadThresholdFoV = Mathf.Tan(Mathf.Deg2Rad * minFov * 0.5f) * maxRange;
                corrected.x = Mathf.Max(corrected.x, deadThresholdFoV);
                corrected.y = Mathf.Max(corrected.y, deadThresholdFoV, Mathf.Epsilon * 100); //prevent any division by zero

                if (!fixedAspect)
                {
                    aspect = corrected.x / corrected.y;
                }
                float min = Mathf.Min(corrected.x, corrected.y);
                if (!fixedFov && maxRange > Mathf.Epsilon * 100)
                {
                    fov = Mathf.Atan(min / maxRange) * 2f * Mathf.Rad2Deg;
                }
            }

            return new Vector4(aspect, fov, maxRange, minRange);
        }

        public static void DrawOrthoFrustumWireframe(Vector4 widthHeightMaxRangeMinRange, float distanceTruncPlane = 0f)
        {
            float halfWidth = widthHeightMaxRangeMinRange.x * 0.5f;
            float halfHeight = widthHeightMaxRangeMinRange.y * 0.5f;
            float maxRange = widthHeightMaxRangeMinRange.z;
            float minRange = widthHeightMaxRangeMinRange.w;

            Vector3 sizeX = new Vector3(halfWidth, 0, 0);
            Vector3 sizeY = new Vector3(0, halfHeight, 0);
            Vector3 nearEnd = new Vector3(0, 0, minRange);
            Vector3 farEnd = new Vector3(0, 0, maxRange);

            Vector3 s1 = nearEnd + sizeX + sizeY;
            Vector3 s2 = nearEnd - sizeX + sizeY;
            Vector3 s3 = nearEnd - sizeX - sizeY;
            Vector3 s4 = nearEnd + sizeX - sizeY;

            Vector3 e1 = farEnd + sizeX + sizeY;
            Vector3 e2 = farEnd - sizeX + sizeY;
            Vector3 e3 = farEnd - sizeX - sizeY;
            Vector3 e4 = farEnd + sizeX - sizeY;

            Handles.DrawLine(s1, s2);
            Handles.DrawLine(s2, s3);
            Handles.DrawLine(s3, s4);
            Handles.DrawLine(s4, s1);

            Handles.DrawLine(e1, e2);
            Handles.DrawLine(e2, e3);
            Handles.DrawLine(e3, e4);
            Handles.DrawLine(e4, e1);

            Handles.DrawLine(s1, e1);
            Handles.DrawLine(s2, e2);
            Handles.DrawLine(s3, e3);
            Handles.DrawLine(s4, e4);

            if (distanceTruncPlane> 0f)
            {
                Vector3 truncPoint = new Vector3(0, 0, distanceTruncPlane);
                Vector3 t1 = truncPoint + sizeX + sizeY;
                Vector3 t2 = truncPoint - sizeX + sizeY;
                Vector3 t3 = truncPoint - sizeX - sizeY;
                Vector3 t4 = truncPoint + sizeX - sizeY;

                Handles.DrawLine(t1, t2);
                Handles.DrawLine(t2, t3);
                Handles.DrawLine(t3, t4);
                Handles.DrawLine(t4, t1);
            }
        }
        public static Vector4 DrawOrthoFrustumHandle(Vector4 widthHeightMaxRangeMinRange, bool useNearHandle)
        {
            float halfWidth = widthHeightMaxRangeMinRange.x * 0.5f;
            float halfHeight = widthHeightMaxRangeMinRange.y * 0.5f;
            float maxRange = widthHeightMaxRangeMinRange.z;
            float minRange = widthHeightMaxRangeMinRange.w;
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            
            if (useNearHandle)
            {
                minRange = SliderLineHandle(Vector3.zero, Vector3.forward, minRange);
            }

            maxRange = SliderLineHandle(Vector3.zero, Vector3.forward, maxRange);

            EditorGUI.BeginChangeCheck();
            halfWidth = SliderLineHandle(farEnd, Vector3.right, halfWidth);
            halfWidth = SliderLineHandle(farEnd, Vector3.left, halfWidth);
            if (EditorGUI.EndChangeCheck())
            {
                halfWidth = Mathf.Max(0f, halfWidth);
            }

            EditorGUI.BeginChangeCheck();
            halfHeight = SliderLineHandle(farEnd, Vector3.up, halfHeight);
            halfHeight = SliderLineHandle(farEnd, Vector3.down, halfHeight);
            if (EditorGUI.EndChangeCheck())
            {
                halfHeight = Mathf.Max(0f, halfHeight);
            }

            return new Vector4(halfWidth * 2f, halfHeight * 2f, maxRange, minRange);
        }
    }
}
