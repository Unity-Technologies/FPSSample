using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MaterialPropertyOverride))]
[CanEditMultipleObjects]
public class MatPropsOverrideEditor : Editor
{
    // Cache of known shaders and their properties
    static Dictionary<int, List<ShaderPropertyInfo>> shaderProps = new Dictionary<int, List<ShaderPropertyInfo>>();

    public class ShaderPropertyInfo
    {
        public string property;
        public ShaderPropertyType type;
        public string description;
        public float rangeMin;
        public float rangeMax;
    }

    // Caches the list of properties
    public static List<ShaderPropertyInfo> GetShaderProperties(Shader s)
    {
        if (shaderProps.ContainsKey(s.GetInstanceID()))
            return shaderProps[s.GetInstanceID()];

        var res = new List<ShaderPropertyInfo>();
        var pc = ShaderUtil.GetPropertyCount(s);
        for (var i = 0; i < pc; i++)
        {
            var sp = new ShaderPropertyInfo();
            sp.property = ShaderUtil.GetPropertyName(s, i);
            sp.type = (ShaderPropertyType)ShaderUtil.GetPropertyType(s, i);
            sp.description = ShaderUtil.GetPropertyDescription(s, i);
            if (sp.type == ShaderPropertyType.Range)
            {
                sp.rangeMin = ShaderUtil.GetRangeLimits(s, i, 1);
                sp.rangeMax = ShaderUtil.GetRangeLimits(s, i, 2);
            }
            res.Add(sp);
        }
        return shaderProps[s.GetInstanceID()] = res;
    }

    public override void OnInspectorGUI()
    {
        var myMatProps = target as MaterialPropertyOverride;

        EditorGUILayout.Space();

        var headStyle = new GUIStyle("ShurikenModuleTitle");
        headStyle.fixedHeight = 20.0f;
        headStyle.contentOffset = new Vector2(5, -2);
        headStyle.font = EditorStyles.boldFont;

        // Draw header for affected renders
        var re = GUILayoutUtility.GetRect(20, 22, headStyle);
        GUI.Box(re, "Affected renderers", headStyle);

        // Draw list of renderers
        EditorGUI.indentLevel += 1;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Renderers"), true);
        if (EditorGUI.EndChangeCheck())
        {
            // Clear out all previously affected renders
            myMatProps.Clear();
            serializedObject.ApplyModifiedProperties();

            // Remove any null elements that may have appeared from user editing
            for (var i = myMatProps.m_Renderers.Count - 1; i >= 0; i--)
            {
                if (myMatProps.m_Renderers[i] == null)
                    myMatProps.m_Renderers.RemoveAt(i);
            }

            // Apply to new list of renderers
            myMatProps.Clear();
            myMatProps.Apply();
        }
        EditorGUI.indentLevel -= 1;

        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Populate from children", "Button"))
        {
            Undo.RecordObject(myMatProps, "Populate");
            myMatProps.Populate();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (myMatProps.m_Renderers.Count == 0)
        {
            EditorGUILayout.HelpBox("No renderers affected!. This component makes no sense without renderers to affect.", MessageType.Error);
            return;
        }

        // Find list of unique materials used in the renderers
        // Not using a Set as we want to keep order from renderers list
        var mats = new List<Material>();
        foreach (var r in myMatProps.m_Renderers)
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m == null)
                    continue;
                if (mats.Contains(m))
                    continue;
                mats.Add(m);
            }
        }

        // Sync mat list with override list
        foreach (var m in mats)
        {
            // Delete overrides that does not match one of our materials
            if (myMatProps.materialOverrides.Find(x => x.material == m) == null)
            {
                var mo = new MaterialPropertyOverride.MaterialOverride();
                mo.material = m;
                myMatProps.materialOverrides.Add(mo);
            }
        }
        // Add overrides for new materials
        myMatProps.materialOverrides.RemoveAll(x => !mats.Contains(x.material));

        // Draw materials
        bool changed = false;
        for(var i = 0; i < myMatProps.materialOverrides.Count; i++)
        {
            var o = myMatProps.materialOverrides[i];

            // Draw material header
            EditorGUILayout.Space();
            re = GUILayoutUtility.GetRect(16f, 22f, headStyle);
            GUI.Box(re, "Material: " + o.material.name + " ("+o.material.shader.name+")", headStyle);

            // Draw enabled toggle and show_all button
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var actProp = serializedObject.FindProperty("materialOverrides.Array.data[" + i + "].active");
            EditorGUILayout.PropertyField(actProp);
            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.FlexibleSpace();
            if(o.active)
                o.showAll = GUILayout.Toggle(o.showAll, "Show all", "Button", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            if (o.active)
            {
                GUILayout.BeginHorizontal();
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Draw MatPropOverrideAsset field
                EditorGUI.BeginChangeCheck();
                var prop = serializedObject.FindProperty("materialOverrides.Array.data[" + i + "].propertyOverrideAsset");
                EditorGUILayout.PropertyField(prop, new GUIContent("Property override: asset"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                if(o.propertyOverrideAsset != null && o.propertyOverrideAsset.shader != o.material.shader)
                {
                    EditorGUILayout.HelpBox("Shader mismatch. The selected override asset does not match this material's shader.", MessageType.Error);
                }

                // Draw list of override properties
                if (Selection.gameObjects.Length > 1)
                    EditorGUILayout.HelpBox("Multi editing not supported", MessageType.Info);
                else
                    changed = changed | DrawOverrideGUI(o.material.shader, o.propertyOverrides, o.showAll, myMatProps);
            }
        }


        if (changed)
        {
            myMatProps.Clear();
            myMatProps.Apply();
        }
    }

    public static bool DrawOverrideGUI(
        Shader shader,
        List<MaterialPropertyOverride.ShaderPropertyValue> propertyOverrides,
        bool showAll,
        UnityEngine.Object target)
    {
        var headStyle = new GUIStyle("ShurikenModuleTitle");
        headStyle.fixedHeight = 20.0f;
        headStyle.contentOffset = new Vector2(5, -2);
        headStyle.font = EditorStyles.boldFont;

        var shaderProperties = GetShaderProperties(shader);

        foreach (var p in shaderProperties)
        {
            // Decide if we should draw
            var propOverride = propertyOverrides.Find(x => x.propertyName == p.property);
            bool hasOverride = propOverride != null;
            if (!hasOverride && !showAll)
                continue;

            GUILayout.BeginHorizontal();
            var buttonPressed = false;
            if (showAll)
                buttonPressed = GUILayout.Button(hasOverride ? "-" : "+", GUILayout.Width(20));
            var desc = new GUIContent(p.description, p.property);
            if (!hasOverride)
            {
                // Draw an non-overridden property. Offer to become overridden
                GUILayout.Label(desc);
                if (buttonPressed)
                {
                    var spv = new MaterialPropertyOverride.ShaderPropertyValue();
                    spv.type = p.type;
                    spv.propertyName = p.property;
                    Undo.RecordObject(target, "Override");
                    propertyOverrides.Add(spv);
                }
            }
            else
            {
                // Draw an overridden property. Offer change of value
                Undo.RecordObject(target, "Override");
                switch (p.type)
                {
                    case ShaderPropertyType.Color:
                        //propOverride.colValue = EditorGUILayout.ColorField(desc, propOverride.colValue);
                        propOverride.colValue = EditorGUILayout.ColorField(desc, propOverride.colValue, false, true, true);
                        break;
                    case ShaderPropertyType.Float:
                        propOverride.floatValue = EditorGUILayout.FloatField(desc, propOverride.floatValue);
                        break;
                    case ShaderPropertyType.Range:
                        propOverride.floatValue = EditorGUILayout.Slider(desc, propOverride.floatValue, p.rangeMin, p.rangeMax);
                        break;
                    case ShaderPropertyType.Vector:
                        propOverride.vecValue = EditorGUILayout.Vector4Field(desc, propOverride.vecValue);
                        break;
                    case ShaderPropertyType.TexEnv:
                        propOverride.texValue = (Texture)EditorGUILayout.ObjectField(desc, propOverride.texValue, typeof(Texture), false);
                        break;
                }
                if (buttonPressed)
                {
                    propertyOverrides.Remove(propOverride);
                }
            }
            GUILayout.EndHorizontal();
        }

        return GUI.changed;
    }
}
