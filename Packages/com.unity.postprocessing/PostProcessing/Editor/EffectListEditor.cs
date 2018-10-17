using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public sealed class EffectListEditor
    {
        public PostProcessProfile asset { get; private set; }
        Editor m_BaseEditor;

        SerializedObject m_SerializedObject;
        SerializedProperty m_SettingsProperty;

        Dictionary<Type, Type> m_EditorTypes; // SettingsType => EditorType
        List<PostProcessEffectBaseEditor> m_Editors;

        public EffectListEditor(Editor editor)
        {
            Assert.IsNotNull(editor);
            m_BaseEditor = editor;
        }

        public void Init(PostProcessProfile asset, SerializedObject serializedObject)
        {
            Assert.IsNotNull(asset);
            Assert.IsNotNull(serializedObject);
            
            this.asset = asset;
            m_SerializedObject = serializedObject;
            m_SettingsProperty = serializedObject.FindProperty("settings");
            Assert.IsNotNull(m_SettingsProperty);

            m_EditorTypes = new Dictionary<Type, Type>();
            m_Editors = new List<PostProcessEffectBaseEditor>();

            // Gets the list of all available postfx editors
            var editorTypes = RuntimeUtilities.GetAllAssemblyTypes()
                                .Where(
                                    t => t.IsSubclassOf(typeof(PostProcessEffectBaseEditor))
                                      && t.IsDefined(typeof(PostProcessEditorAttribute), false)
                                      && !t.IsAbstract
                                );

            // Map them to their corresponding settings type
            foreach (var editorType in editorTypes)
            {
                var attribute = editorType.GetAttribute<PostProcessEditorAttribute>();
                m_EditorTypes.Add(attribute.settingsType, editorType);
            }

            // Create editors for existing settings
            for (int i = 0; i < this.asset.settings.Count; i++)
                CreateEditor(this.asset.settings[i], m_SettingsProperty.GetArrayElementAtIndex(i));

            // Keep track of undo/redo to redraw the inspector when that happens
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            asset.isDirty = true;

            // Dumb hack to make sure the serialized object is up to date on undo (else there'll be
            // a state mismatch when this class is used in a GameObject inspector).
            m_SerializedObject.Update();
            m_SerializedObject.ApplyModifiedProperties();

            // Seems like there's an issue with the inspector not repainting after some undo events
            // This will take care of that
            m_BaseEditor.Repaint();
        }

        void CreateEditor(PostProcessEffectSettings settings, SerializedProperty property, int index = -1)
        {
            var settingsType = settings.GetType();
            Type editorType;

            if (!m_EditorTypes.TryGetValue(settingsType, out editorType))
                editorType = typeof(DefaultPostProcessEffectEditor);

            var editor = (PostProcessEffectBaseEditor)Activator.CreateInstance(editorType);
            editor.Init(settings, m_BaseEditor);
            editor.baseProperty = property.Copy();

            if (index < 0)
                m_Editors.Add(editor);
            else
                m_Editors[index] = editor;
        }

        // Clears & recreate all editors - mainly used when the volume has been modified outside of
        // the editor (user scripts, inspector reset etc).
        void RefreshEditors()
        {
            // Disable all editors first
            foreach (var editor in m_Editors)
                editor.OnDisable();

            // Remove them
            m_Editors.Clear();

            // Recreate editors for existing settings, if any
            for (int i = 0; i < asset.settings.Count; i++)
                CreateEditor(asset.settings[i], m_SettingsProperty.GetArrayElementAtIndex(i));
        }

        public void Clear()
        {
            if (m_Editors == null)
                return; // Hasn't been inited yet

            foreach (var editor in m_Editors)
                editor.OnDisable();

            m_Editors.Clear();
            m_EditorTypes.Clear();

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        public void OnGUI()
        {
            if (asset == null)
                return;

            if (asset.isDirty)
            {
                RefreshEditors();
                asset.isDirty = false;
            }

            bool isEditable = !VersionControl.Provider.isActive
                || AssetDatabase.IsOpenForEdit(asset, StatusQueryOptions.UseCachedIfPossible);

            using (new EditorGUI.DisabledScope(!isEditable))
            {
                EditorGUILayout.LabelField(EditorUtilities.GetContent("Overrides"), EditorStyles.boldLabel);

                // Override list
                for (int i = 0; i < m_Editors.Count; i++)
                {
                    var editor = m_Editors[i];
                    string title = editor.GetDisplayTitle();
                    int id = i; // Needed for closure capture below

                    EditorUtilities.DrawSplitter();
                    bool displayContent = EditorUtilities.DrawHeader(
                        title,
                        editor.baseProperty,
                        editor.activeProperty,
                        editor.target,
                        () => ResetEffectOverride(editor.target.GetType(), id),
                        () => RemoveEffectOverride(id)
                        );

                    if (displayContent)
                    {
                        using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                            editor.OnInternalInspectorGUI();
                    }
                }

                if (m_Editors.Count > 0)
                {
                    EditorUtilities.DrawSplitter();
                    EditorGUILayout.Space();
                }
                else
                {
                    EditorGUILayout.HelpBox("No override set on this volume.", MessageType.Info);
                }

                if (GUILayout.Button("Add effect...", EditorStyles.miniButton))
                {
                    var menu = new GenericMenu();

                    var typeMap = PostProcessManager.instance.settingsTypes;
                    foreach (var kvp in typeMap)
                    {
                        var type = kvp.Key;
                        var title = EditorUtilities.GetContent(kvp.Value.menuItem);
                        bool exists = asset.HasSettings(type);

                        if (!exists)
                            menu.AddItem(title, false, () => AddEffectOverride(type));
                        else
                            menu.AddDisabledItem(title);
                    }

                    menu.ShowAsContext();
                }

                EditorGUILayout.Space();
            }
        }

        void AddEffectOverride(Type type)
        {
            m_SerializedObject.Update();

            var effect = CreateNewEffect(type);
            Undo.RegisterCreatedObjectUndo(effect, "Add Effect Override");

            // Store this new effect as a subasset so we can reference it safely afterwards. Only when its not an instantiated profile
            if (EditorUtility.IsPersistent(asset))
                AssetDatabase.AddObjectToAsset(effect, asset);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_SettingsProperty.arraySize++;
            var effectProp = m_SettingsProperty.GetArrayElementAtIndex(m_SettingsProperty.arraySize - 1);
            effectProp.objectReferenceValue = effect;

            // Create & store the internal editor object for this effect
            CreateEditor(effect, effectProp);

            m_SerializedObject.ApplyModifiedProperties();

            // Force save / refresh. Important to do this last because SaveAssets can cause effect to become null!
            if (EditorUtility.IsPersistent(asset))
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }

        void RemoveEffectOverride(int id)
        {
            // Huh. Hack to keep foldout state on the next element...
            bool nextFoldoutState = false;
            if (id < m_Editors.Count - 1)
                nextFoldoutState = m_Editors[id + 1].baseProperty.isExpanded;

            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors.RemoveAt(id);

            m_SerializedObject.Update();

            var property = m_SettingsProperty.GetArrayElementAtIndex(id);
            var effect = property.objectReferenceValue;

            // Unassign it (should be null already but serialization does funky things
            property.objectReferenceValue = null;

            // ...and remove the array index itself from the list
            m_SettingsProperty.DeleteArrayElementAtIndex(id);

            // Finally refresh editor reference to the serialized settings list
            for (int i = 0; i < m_Editors.Count; i++)
                m_Editors[i].baseProperty = m_SettingsProperty.GetArrayElementAtIndex(i).Copy();

            if (id < m_Editors.Count)
                m_Editors[id].baseProperty.isExpanded = nextFoldoutState;
            
            m_SerializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            Undo.DestroyObjectImmediate(effect);

            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        // Reset is done by deleting and removing the object from the list and adding a new one in
        // the place as it was before
        void ResetEffectOverride(Type type, int id)
        {
            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors[id] = null;

            m_SerializedObject.Update();

            var property = m_SettingsProperty.GetArrayElementAtIndex(id);
            var prevSettings = property.objectReferenceValue;

            // Unassign it but down remove it from the array to keep the index available
            property.objectReferenceValue = null;

            // Create a new object
            var newEffect = CreateNewEffect(type);
            Undo.RegisterCreatedObjectUndo(newEffect, "Reset Effect Override");

            // Store this new effect as a subasset so we can reference it safely afterwards
            AssetDatabase.AddObjectToAsset(newEffect, asset);

            // Put it in the reserved space
            property.objectReferenceValue = newEffect;

            // Create & store the internal editor object for this effect
            CreateEditor(newEffect, property, id);

            m_SerializedObject.ApplyModifiedProperties();

            // Same as RemoveEffectOverride, destroy at the end so it's recreated first on Undo to
            // make sure the GUID exists before undoing the list state
            Undo.DestroyObjectImmediate(prevSettings);
            
            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        PostProcessEffectSettings CreateNewEffect(Type type)
        {
            var effect = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            effect.name = type.Name;
            effect.enabled.value = true;
            return effect;
        }
    }
}
