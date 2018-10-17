using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public sealed class VolumeComponentListEditor
    {
        public VolumeProfile asset { get; private set; }

        Editor m_BaseEditor;

        SerializedObject m_SerializedObject;
        SerializedProperty m_ComponentsProperty;

        Dictionary<Type, Type> m_EditorTypes; // Component type => Editor type
        List<VolumeComponentEditor> m_Editors;

        static VolumeComponent s_ClipboardContent;

        public VolumeComponentListEditor(Editor editor)
        {
            Assert.IsNotNull(editor);
            m_BaseEditor = editor;
        }

        public void Init(VolumeProfile asset, SerializedObject serializedObject)
        {
            Assert.IsNotNull(asset);
            Assert.IsNotNull(serializedObject);

            this.asset = asset;
            m_SerializedObject = serializedObject;
            m_ComponentsProperty = serializedObject.Find((VolumeProfile x) => x.components);
            Assert.IsNotNull(m_ComponentsProperty);

            m_EditorTypes = new Dictionary<Type, Type>();
            m_Editors = new List<VolumeComponentEditor>();

            // Gets the list of all available component editors
            var editorTypes = CoreUtils.GetAllAssemblyTypes()
                .Where(
                    t => t.IsSubclassOf(typeof(VolumeComponentEditor))
                    && t.IsDefined(typeof(VolumeComponentEditorAttribute), false)
                    && !t.IsAbstract
                    );

            // Map them to their corresponding component type
            foreach (var editorType in editorTypes)
            {
                var attribute = (VolumeComponentEditorAttribute)editorType.GetCustomAttributes(typeof(VolumeComponentEditorAttribute), false)[0];
                m_EditorTypes.Add(attribute.componentType, editorType);
            }

            // Create editors for existing components
            var components = asset.components;
            for (int i = 0; i < components.Count; i++)
                CreateEditor(components[i], m_ComponentsProperty.GetArrayElementAtIndex(i));

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

        // index is only used when we need to re-create a component in a specific spot (e.g. reset)
        void CreateEditor(VolumeComponent component, SerializedProperty property, int index = -1, bool forceOpen = false)
        {
            var componentType = component.GetType();
            Type editorType;

            if (!m_EditorTypes.TryGetValue(componentType, out editorType))
                editorType = typeof(VolumeComponentEditor);

            var editor = (VolumeComponentEditor)Activator.CreateInstance(editorType);
            editor.Init(component, m_BaseEditor);
            editor.baseProperty = property.Copy();

            if (forceOpen)
                editor.baseProperty.isExpanded = true;

            if (index < 0)
                m_Editors.Add(editor);
            else
                m_Editors[index] = editor;
        }

        void RefreshEditors()
        {
            // Disable all editors first
            foreach (var editor in m_Editors)
                editor.OnDisable();

            // Remove them
            m_Editors.Clear();

            // Recreate editors for existing settings, if any
            var components = asset.components;
            for (int i = 0; i < components.Count; i++)
                CreateEditor(components[i], m_ComponentsProperty.GetArrayElementAtIndex(i));
        }

        public void Clear()
        {
            if (m_Editors == null)
                return; // Hasn't been inited yet

            foreach (var editor in m_Editors)
                editor.OnDisable();

            m_Editors.Clear();
            m_EditorTypes.Clear();

            // ReSharper disable once DelegateSubtraction
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
                // Component list
                for (int i = 0; i < m_Editors.Count; i++)
                {
                    var editor = m_Editors[i];
                    string title = editor.GetDisplayTitle();
                    int id = i; // Needed for closure capture below

                    CoreEditorUtils.DrawSplitter();
                    bool displayContent = CoreEditorUtils.DrawHeaderToggle(
                            title,
                            editor.baseProperty,
                            editor.activeProperty,
                            pos => OnContextClick(pos, editor.target, id)
                            );

                    if (displayContent)
                    {
                        using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                            editor.OnInternalInspectorGUI();
                    }
                }

                if (m_Editors.Count > 0)
                    CoreEditorUtils.DrawSplitter();
                else
                    EditorGUILayout.HelpBox("No override set on this volume. Drop a component here or use the Add button.", MessageType.Info);

                EditorGUILayout.Space();

                using (var hscope = new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(CoreEditorUtils.GetContent("Add component overrides..."), EditorStyles.miniButton))
                    {
                        var r = hscope.rect;
                        var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                        FilterWindow.Show(pos, new VolumeComponentProvider(asset, this));
                    }
                }
            }
        }

        void OnContextClick(Vector2 position, VolumeComponent targetComponent, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(CoreEditorUtils.GetContent("Move Up"));
            else
                menu.AddItem(CoreEditorUtils.GetContent("Move Up"), false, () => MoveComponent(id, -1));

            if (id == m_Editors.Count - 1)
                menu.AddDisabledItem(CoreEditorUtils.GetContent("Move Down"));
            else
                menu.AddItem(CoreEditorUtils.GetContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(CoreEditorUtils.GetContent("Reset"), false, () => ResetComponent(targetComponent.GetType(), id));
            menu.AddItem(CoreEditorUtils.GetContent("Remove"), false, () => RemoveComponent(id));
            menu.AddSeparator(string.Empty);
            menu.AddItem(CoreEditorUtils.GetContent("Copy Settings"), false, () => CopySettings(targetComponent));

            if (CanPaste(targetComponent))
                menu.AddItem(CoreEditorUtils.GetContent("Paste Settings"), false, () => PasteSettings(targetComponent));
            else
                menu.AddDisabledItem(CoreEditorUtils.GetContent("Paste Settings"));

            menu.AddSeparator(string.Empty);
            menu.AddItem(CoreEditorUtils.GetContent("Toggle All"), false, () => m_Editors[id].SetAllOverridesTo(true));
            menu.AddItem(CoreEditorUtils.GetContent("Toggle None"), false, () => m_Editors[id].SetAllOverridesTo(false));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        VolumeComponent CreateNewComponent(Type type)
        {
            var effect = (VolumeComponent)ScriptableObject.CreateInstance(type);
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            effect.name = type.Name;
            return effect;
        }

// sample-game begin: fix against SaveAssets() calling GC and destroying assets not referenced
        private ScriptableObject m_TempRefToAvoidGettingDestroyed;
        internal void AddComponent(Type type)
        {
            m_SerializedObject.Update();

            var component = CreateNewComponent(type);
            Undo.RegisterCreatedObjectUndo(component, "Add Volume Component");

            // Store this new effect as a subasset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(asset))
                AssetDatabase.AddObjectToAsset(component, asset);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_ComponentsProperty.arraySize++;
            var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(m_ComponentsProperty.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Force save / refresh
            if (EditorUtility.IsPersistent(asset))
            {
                EditorUtility.SetDirty(asset);
                m_TempRefToAvoidGettingDestroyed = component;
                AssetDatabase.SaveAssets();
                m_TempRefToAvoidGettingDestroyed = null;
            }
// sample-game end

            // Create & store the internal editor object for this effect
            CreateEditor(component, componentProp, forceOpen: true);

            m_SerializedObject.ApplyModifiedProperties();
        }

        internal void RemoveComponent(int id)
        {
            // Huh. Hack to keep foldout state on the next element...
            bool nextFoldoutState = false;
            if (id < m_Editors.Count - 1)
                nextFoldoutState = m_Editors[id + 1].baseProperty.isExpanded;

            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors.RemoveAt(id);

            m_SerializedObject.Update();

            var property = m_ComponentsProperty.GetArrayElementAtIndex(id);
            var component = property.objectReferenceValue;

            // Unassign it (should be null already but serialization does funky things
            property.objectReferenceValue = null;

            // ...and remove the array index itself from the list
            m_ComponentsProperty.DeleteArrayElementAtIndex(id);

            // Finally refresh editor reference to the serialized settings list
            for (int i = 0; i < m_Editors.Count; i++)
                m_Editors[i].baseProperty = m_ComponentsProperty.GetArrayElementAtIndex(i).Copy();

            // Set the proper foldout state if needed
            if (id < m_Editors.Count)
                m_Editors[id].baseProperty.isExpanded = nextFoldoutState;

            m_SerializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            Undo.DestroyObjectImmediate(component);

            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        // Reset is done by deleting and removing the object from the list and adding a new one in
        // the same spot as it was before
        internal void ResetComponent(Type type, int id)
        {
            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors[id] = null;

            m_SerializedObject.Update();

            var property = m_ComponentsProperty.GetArrayElementAtIndex(id);
            var prevComponent = property.objectReferenceValue;

            // Unassign it but down remove it from the array to keep the index available
            property.objectReferenceValue = null;

            // Create a new object
            var newComponent = CreateNewComponent(type);
            Undo.RegisterCreatedObjectUndo(newComponent, "Reset Volume Component");

            // Store this new effect as a subasset so we can reference it safely afterwards
            AssetDatabase.AddObjectToAsset(newComponent, asset);

            // Put it in the reserved space
            property.objectReferenceValue = newComponent;

            // Create & store the internal editor object for this effect
            CreateEditor(newComponent, property, id);

            m_SerializedObject.ApplyModifiedProperties();

            // Same as RemoveComponent, destroy at the end so it's recreated first on Undo to make
            // sure the GUID exists before undoing the list state
            Undo.DestroyObjectImmediate(prevComponent);

            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        internal void MoveComponent(int id, int offset)
        {
            // Move components
            m_SerializedObject.Update();
            m_ComponentsProperty.MoveArrayElement(id, id + offset);
            m_SerializedObject.ApplyModifiedProperties();

            // Move editors
            var prev = m_Editors[id + offset];
            m_Editors[id + offset] = m_Editors[id];
            m_Editors[id] = prev;
        }

        // Copy/pasting is simply done by creating an in memory copy of the selected component and
        // copying over the serialized data to another; it doesn't use nor affect the OS clipboard
        static bool CanPaste(VolumeComponent targetComponent)
        {
            return s_ClipboardContent != null
                && s_ClipboardContent.GetType() == targetComponent.GetType();
        }

        static void CopySettings(VolumeComponent targetComponent)
        {
            if (s_ClipboardContent != null)
            {
                CoreUtils.Destroy(s_ClipboardContent);
                s_ClipboardContent = null;
            }

            s_ClipboardContent = (VolumeComponent)ScriptableObject.CreateInstance(targetComponent.GetType());
            EditorUtility.CopySerializedIfDifferent(targetComponent, s_ClipboardContent);
        }

        static void PasteSettings(VolumeComponent targetComponent)
        {
            Assert.IsNotNull(s_ClipboardContent);
            Assert.AreEqual(s_ClipboardContent.GetType(), targetComponent.GetType());

            Undo.RecordObject(targetComponent, "Paste Settings");
            EditorUtility.CopySerializedIfDifferent(s_ClipboardContent, targetComponent);
        }
    }
}
