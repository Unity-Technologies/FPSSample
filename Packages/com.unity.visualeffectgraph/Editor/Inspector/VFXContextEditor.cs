using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using Object = UnityEngine.Object;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(VFXContext), true)]
[CanEditMultipleObjects]
public class VFXContextEditor : VFXSlotContainerEditor
{
    SerializedProperty spaceProperty;
    SerializedObject dataObject;

    float m_Width;


    VFXViewController m_ViewController;
    VFXContextController m_ContextController;

    protected new void OnEnable()
    {
        UnityEngine.Object[] allData = targets.Cast<VFXContext>().Select(t => t.GetData()).Distinct().Where(t => t != null).Cast<UnityEngine.Object>().ToArray();
        if (allData.Length > 0)
        {
            dataObject = new SerializedObject(allData);

            spaceProperty = dataObject.FindProperty("m_Space");
        }
        else
        {
            dataObject = null;
            spaceProperty = null;
        }

        if (!serializedObject.isEditingMultipleObjects)
        {
            m_ViewController = VFXViewController.GetController(((VFXContext)target).GetGraph().GetResource());
            m_ViewController.useCount++;

            m_ContextController = m_ViewController.GetRootNodeController((VFXContext)target, 0) as VFXContextController;
        }

        base.OnEnable();
    }

    private new void OnDisable()
    {
        base.OnDisable();

        if(m_ViewController != null)
        {
            m_ViewController.useCount--;
            m_ViewController = null;
        }
    }

    public override void DoInspectorGUI()
    {
        if (spaceProperty != null)
            EditorGUILayout.PropertyField(spaceProperty);

        base.DoInspectorGUI();
    }

    void DoAttributeLayoutGUI(string label, StructureOfArrayProvider.BucketInfo[] layout)
    {
        GUILayout.Label(label, Styles.header);

        // Used to distribute width evenly for each cell, induces a one-frame latency
        var w = GUILayoutUtility.GetLastRect().width;
        if (Event.current.type != EventType.Layout && w > 0)
            m_Width = w - 48;

        int maxSize = 0;

        foreach (StructureOfArrayProvider.BucketInfo bucket in layout)
            maxSize = Math.Max(maxSize, bucket.size);

        DrawAttributeLayoutGrid(layout, maxSize);
    }

    void DrawAttributeLayoutGrid(StructureOfArrayProvider.BucketInfo[] layout, int maxSize)
    {
        int i = 0;
        float height = 16.0f;

        Rect r = GUILayoutUtility.GetRect(m_Width, layout.Length * height);

        foreach (var bucket in layout)
        {
            float x = r.x;
            float y = r.y + i * height;
            float cellwidth = (m_Width - 16) / maxSize;

            Rect cellRect = new Rect(x, y, 16, height);
            GUI.Label(cellRect, i.ToString(), Styles.cell);

            int bucketSize = bucket.size;
            int usedSize = bucket.usedSize;

            x += 16;

            for (int j = 0; j < maxSize; j++)
            {
                cellRect = new Rect(x, y, cellwidth, height);
                if (j < usedSize)
                {
                    var attrib = bucket.attributes[j];
                    if (attrib.name != null)
                        Styles.DataTypeLabel(cellRect, attrib.name, attrib.type, Styles.cell);
                    else
                        Styles.DataTypeLabel(cellRect, "", VFXValueType.None, Styles.cell);
                }
                else
                {
                    if (j < bucketSize)
                        Styles.DataTypeLabel(cellRect, "", VFXValueType.None, Styles.cell);
                    else
                        GUI.Label(cellRect, "");
                }
                x += cellwidth;
            }
            i++;
        }
    }

    public override void OnInspectorGUI()
    {
        if (dataObject != null)
            dataObject.Update();

        if (m_ContextController != null && m_ContextController.letter != '\0')
        {
            GUILayout.Label(m_ContextController.letter.ToString(),Styles.letter);
        }

        base.OnInspectorGUI();

        if (dataObject != null)
            if (dataObject.ApplyModifiedProperties())
            {
                foreach (VFXContext ctx in targets.OfType<VFXContext>())
                {
                    // notify that something changed.
                    ctx.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                }
            }

        if (serializedObject.isEditingMultipleObjects) return; // Summary Only visible for single selection



        // Context / SystemData
        if (dataObject == null) return;
        var context = (VFXContext)target;
        var data = (VFXData)dataObject.targetObject;

        // Particle context data
        if (data.type == VFXDataType.kParticle)
        {
            VFXDataParticle particleData = data as VFXDataParticle;
            EditorGUILayout.Space();
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Capacity", particleData.capacity.ToString());

                EditorGUILayout.Space();

                var attributes = data.GetAttributes();

                if (attributes.Count() > 0)
                {
                    EditorGUILayout.LabelField("System Attribute Summary", Styles.header);

                    foreach (var attr in attributes)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(attr.attrib.name, Styles.cell);
                            Styles.DataTypeLabel(attr.attrib.type.ToString(), attr.attrib.type, Styles.cell, GUILayout.Width(64));
                            int size = VFXExpressionHelper.GetSizeOfType(attr.attrib.type) * 4;
                            GUILayout.Label(size + " byte" + (size > 1 ? "s" : "") , Styles.cell, GUILayout.Width(64));
                            var mode = attr.mode;
                            GUILayout.Label(mode.ToString(), Styles.cell, GUILayout.Width(64));
                        }
                    }
                }

                StructureOfArrayProvider.BucketInfo[] current = particleData.GetCurrentAttributeLayout();
                StructureOfArrayProvider.BucketInfo[] source = particleData.GetSourceAttributeLayout();

                if (current.Length > 0)
                {
                    GUILayout.Space(24);
                    DoAttributeLayoutGUI("Current Attribute Layout", current);
                }

                if (source.Length > 0)
                {
                    GUILayout.Space(12);
                    DoAttributeLayoutGUI("Source Attribute Layout", source);
                }
            }
        }

        if (VFXViewPreference.displayExtraDebugInfo)
        {
            // Extra debug data
            EditorGUILayout.Space();
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Context Type", context.contextType.ToString());
                Styles.Row(Styles.cell, "Task Type", context.taskType.ToString());
                Styles.Row(Styles.cell, "Input Data Type", context.inputType.ToString());
                Styles.Row(Styles.cell, "Context Data Type", data.type.ToString());
                Styles.Row(Styles.cell, "Can Be Compiled", context.CanBeCompiled().ToString());

                EditorGUILayout.Space();

                var attributeInfos = data.GetAttributesForContext(context);
                VFXAttributeInfo[] infos;

                // Early check for context consistency
                try
                {
                    infos = attributeInfos.ToArray();
                }
                catch
                {
                    EditorGUILayout.HelpBox("Context is not connected or results in invalid system, please ensure all flow connections are correct.", MessageType.Warning, true);
                    return;
                }

                EditorGUILayout.LabelField("Attributes used by Context", Styles.header);

                foreach (var info in infos)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(info.attrib.name, Styles.cell);
                        Styles.DataTypeLabel(info.attrib.type.ToString(), info.attrib.type, Styles.cell, GUILayout.Width(80));
                        Styles.AttributeModeLabel(info.mode.ToString(), info.mode, Styles.cell, GUILayout.Width(80));
                    }
                }

                EditorGUILayout.Space();

                Styles.Row(Styles.header, "Blocks");
                foreach (var block in context.activeChildrenWithImplicit)
                    Styles.Row(Styles.cell, block.name, !context.children.Contains(block) ? "implicit" : "");

                EditorGUILayout.Space();
            }
        }
    }
}
