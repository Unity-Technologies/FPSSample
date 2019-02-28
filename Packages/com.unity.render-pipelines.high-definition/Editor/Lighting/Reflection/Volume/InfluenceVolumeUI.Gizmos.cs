using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        [Flags]
        public enum HandleType
        {
            None = 0,
            Base = 1,
            Influence = 1 << 1,
            InfluenceNormal = 1 << 2,

            All = ~0
        }

        public static void DrawGizmos(InfluenceVolumeUI s, InfluenceVolume d, Matrix4x4 matrix, HandleType editedHandle, HandleType showedHandle)
        {
            using (new Handles.DrawingScope(matrix))
            {
                switch (d.shape)
                {
                    case InfluenceShape.Box:
                        if ((showedHandle & HandleType.Base) != 0)
                        {
                            s.boxBaseHandle.center = d.offset;
                            s.boxBaseHandle.size = d.boxSize;
                            s.boxBaseHandle.DrawHull((editedHandle & HandleType.Base) != 0);
                        }
                        if ((showedHandle & HandleType.Influence) != 0)
                        {
                            s.boxInfluenceHandle.monoHandle = !s.data.editorAdvancedModeEnabled.boolValue;
                            s.boxInfluenceHandle.center = d.offset + d.boxBlendOffset;
                            s.boxInfluenceHandle.size = d.boxSize + d.boxBlendSize;
                            s.boxInfluenceHandle.DrawHull((editedHandle & HandleType.Influence) != 0);
                        }
                        if ((showedHandle & HandleType.InfluenceNormal) != 0)
                        {
                            s.boxInfluenceNormalHandle.monoHandle = !s.data.editorAdvancedModeEnabled.boolValue;
                            s.boxInfluenceNormalHandle.center = d.offset + d.boxBlendNormalOffset;
                            s.boxInfluenceNormalHandle.size = d.boxSize + d.boxBlendNormalSize;
                            s.boxInfluenceNormalHandle.DrawHull((editedHandle & HandleType.InfluenceNormal) != 0);
                        }
                        break;

                    case InfluenceShape.Sphere:
                        if ((showedHandle & HandleType.Base) != 0)
                        {
                            s.sphereBaseHandle.center = d.offset;
                            s.sphereBaseHandle.radius = d.sphereRadius;
                            s.sphereBaseHandle.DrawHull((editedHandle & HandleType.Base) != 0);
                        }
                        if ((showedHandle & HandleType.Influence) != 0)
                        {
                            s.sphereInfluenceHandle.center = d.offset;
                            s.sphereInfluenceHandle.radius = d.sphereRadius - d.sphereBlendDistance;
                            s.sphereInfluenceHandle.DrawHull((editedHandle & HandleType.Influence) != 0);
                        }
                        if ((showedHandle & HandleType.InfluenceNormal) != 0)
                        {
                            s.sphereInfluenceNormalHandle.center = d.offset;
                            s.sphereInfluenceNormalHandle.radius = d.sphereRadius - d.sphereBlendNormalDistance;
                            s.sphereInfluenceNormalHandle.DrawHull((editedHandle & HandleType.InfluenceNormal) != 0);
                        }
                        break;
                }
            }
        }
    }
}
