using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        [Flags]
        internal enum ToolBar
        {
            InfluenceShape = 1 << 0,
            Blend = 1 << 1,
            NormalBlend = 1 << 2,
            CapturePosition = 1 << 3
        }
        protected ToolBar[] toolBars = null;    //to be set in child constructor

        protected const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        protected const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        protected const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        protected const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.GridMove;
        //Note: EditMode.SceneViewEditMode.ReflectionProbeOrigin is still used
        //by legacy reflection probe and have its own mecanism that we don't want

        internal static bool IsProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditBaseShape
                || editMode == EditInfluenceShape
                || editMode == EditInfluenceNormalShape
                || editMode == EditCenter;
        }

        static Dictionary<ToolBar, EditMode.SceneViewEditMode> s_Toolbar_Mode = null;
        protected static Dictionary<ToolBar, EditMode.SceneViewEditMode> toolbar_Mode
        {
            get
            {
                return s_Toolbar_Mode ?? (s_Toolbar_Mode = new Dictionary<ToolBar, EditMode.SceneViewEditMode>
                {
                    { ToolBar.InfluenceShape,  EditBaseShape },
                    { ToolBar.Blend,  EditInfluenceShape },
                    { ToolBar.NormalBlend,  EditInfluenceNormalShape },
                    { ToolBar.CapturePosition,  EditCenter }
                });
            }
        }

        //[TODO] change this to be modifiable shortcuts
        static Dictionary<KeyCode, ToolBar> s_Toolbar_ShortCutKey = null;
        protected static Dictionary<KeyCode, ToolBar> toolbar_ShortCutKey
        {
            get
            {
                return s_Toolbar_ShortCutKey ?? (s_Toolbar_ShortCutKey = new Dictionary<KeyCode, ToolBar>
                {
                    { KeyCode.Alpha1, ToolBar.InfluenceShape },
                    { KeyCode.Alpha2, ToolBar.Blend },
                    { KeyCode.Alpha3, ToolBar.NormalBlend },
                    { KeyCode.Alpha4, ToolBar.CapturePosition }
                });
            }
        }

        protected static void Drawer_Toolbars(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            foreach (ToolBar toolBar in s.toolBars)
            {
                List<EditMode.SceneViewEditMode> listMode = new List<EditMode.SceneViewEditMode>();
                List<GUIContent> listContent = new List<GUIContent>();
                if ((toolBar & ToolBar.InfluenceShape) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.InfluenceShape]);
                    listContent.Add(toolbar_Contents[ToolBar.InfluenceShape]);
                }
                if ((toolBar & ToolBar.Blend) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.Blend]);
                    listContent.Add(toolbar_Contents[ToolBar.Blend]);
                }
                if ((toolBar & ToolBar.NormalBlend) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.NormalBlend]);
                    listContent.Add(toolbar_Contents[ToolBar.NormalBlend]);
                }
                if ((toolBar & ToolBar.CapturePosition) > 0)
                {
                    listMode.Add(toolbar_Mode[ToolBar.CapturePosition]);
                    listContent.Add(toolbar_Contents[ToolBar.CapturePosition]);
                }
                EditMode.DoInspectorToolbar(listMode.ToArray(), listContent.ToArray(), GetBoundsGetter(o), o);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        static internal void Drawer_ToolBarButton(ToolBar button, Editor owner, params GUILayoutOption[] options)
        {
            bool enabled = toolbar_Mode[button] == EditMode.editMode;
            EditorGUI.BeginChangeCheck();
            enabled = GUILayout.Toggle(enabled, toolbar_Contents[button], EditorStyles.miniButton, options);
            if (EditorGUI.EndChangeCheck())
            {
                EditMode.SceneViewEditMode targetMode = EditMode.editMode == toolbar_Mode[button] ? EditMode.SceneViewEditMode.None : toolbar_Mode[button];
                EditMode.ChangeEditMode(targetMode, GetBoundsGetter(owner)(), owner);
            }
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
            {
                var bounds = new Bounds();
                foreach (Component targetObject in o.targets)
                {
                    var rp = targetObject.transform;
                    var b = rp.position;
                    bounds.Encapsulate(b);
                }
                return bounds;
            };
        }

        public void DoShortcutKey(Editor owner)
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown || !evt.shift)
                return;

            ToolBar toolbar;
            if (toolbar_ShortCutKey.TryGetValue(evt.keyCode, out toolbar))
            {
                bool used = false;
                foreach (ToolBar t in toolBars)
                {
                    if ((t & toolbar) > 0)
                    {
                        used = true;
                        break;
                    }
                }
                if (!used)
                {
                    return;
                }

                var targetMode = toolbar_Mode[toolbar];
                var mode = EditMode.editMode == targetMode ? EditMode.SceneViewEditMode.None : targetMode;
                EditMode.ChangeEditMode(mode, GetBoundsGetter(owner)(), owner);
                evt.Use();
            }
        }
    }
}
