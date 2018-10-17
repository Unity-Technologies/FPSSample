using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal partial class HDReflectionProbeUI : HDProbeUI
    {
        internal HDReflectionProbeUI()
        {
            toolBars = new[] { ToolBar.InfluenceShape | ToolBar.Blend | ToolBar.NormalBlend, ToolBar.CapturePosition };
        }
    }
}
