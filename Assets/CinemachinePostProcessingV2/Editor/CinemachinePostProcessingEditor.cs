// NOTE: If you are getting errors of the sort that say something like:
//     "The type or namespace name `PostProcessing' does not exist in the namespace"
// it is because the PostProcessing v2 module has been removed from your project.
//
// To make the errors go away, you can either:
//   1 - Download PostProcessing V2 and install it into your project
// or
//   2 - Go into PlayerSettings and remove the define for UNITY_POST_PROCESSING_STACK_V2
//

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;

namespace Cinemachine.PostFX.Editor
{
    [CustomEditor(typeof(CinemachinePostProcessing))]
    public sealed class CinemachinePostProcessingEditor 
        : Cinemachine.Editor.BaseEditor<CinemachinePostProcessing>
    {
        SerializedProperty m_Profile;
        SerializedProperty m_FocusTracksTarget;
        SerializedProperty m_FocusOffset;

        EffectListEditor m_EffectList;
        GUIContent m_ProfileLabel;

        void OnEnable()
        {
            Texture textue = Resources.Load("PostProcessLayer") as Texture;
            m_ProfileLabel = new GUIContent("Profile", textue, "A reference to a profile asset");

            m_FocusTracksTarget = FindProperty(x => x.m_FocusTracksTarget);
            m_FocusOffset = FindProperty(x => x.m_FocusOffset);
            m_Profile = FindProperty(x => x.m_Profile);

            m_EffectList = new EffectListEditor(this);
            RefreshEffectListEditor(Target.m_Profile);
        }

        void OnDisable()
        {
            if (m_EffectList != null)
                m_EffectList.Clear();
        }

        void RefreshEffectListEditor(PostProcessProfile asset)
        {
            if (m_EffectList == null)
                m_EffectList = new EffectListEditor(this);
            m_EffectList.Clear();
            if (asset != null)
                m_EffectList.Init(asset, new SerializedObject(asset));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight); rect.y += 2;
            float checkboxWidth = rect.height + 5;
            rect = EditorGUI.PrefixLabel(rect, new GUIContent(m_FocusTracksTarget.displayName));
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, checkboxWidth, rect.height), m_FocusTracksTarget, GUIContent.none);
            rect.x += checkboxWidth; rect.width -= checkboxWidth;
            if (m_FocusTracksTarget.boolValue)
            {
                GUIContent offsetText = new GUIContent("Offset ");
                var textDimensions = GUI.skin.label.CalcSize(offsetText);
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = textDimensions.x;
                EditorGUI.PropertyField(rect, m_FocusOffset, offsetText);
                EditorGUIUtility.labelWidth = oldWidth;

                bool valid = false;
                DepthOfField dof;
                if (Target.m_Profile != null && Target.m_Profile.TryGetSettings<DepthOfField>(out dof))
                    valid = dof.enabled && dof.active && dof.focusDistance.overrideState 
                        && Target.VirtualCamera.LookAt != null;
                if (!valid)
                    EditorGUILayout.HelpBox(
                        "Focus Tracking requires a LookAt target on the Virtual Camera, and an active DepthOfField/FocusDistance effect in the profile", 
                        MessageType.Warning);
            }

            DrawProfileInspectorGUI();
            Target.InvalidateCachedProfile();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawProfileInspectorGUI()
        {
            EditorGUILayout.Space();

            bool assetHasChanged = false;
            bool showCopy = m_Profile.objectReferenceValue != null;

            // The layout system sort of break alignement when mixing inspector fields with custom
            // layouted fields, do the layout manually instead
            int buttonWidth = showCopy ? 45 : 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (showCopy ? 2 : 1), lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            var buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, m_ProfileLabel);

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                m_Profile.objectReferenceValue 
                    = (PostProcessProfile)EditorGUI.ObjectField(
                        fieldRect, m_Profile.objectReferenceValue, typeof(PostProcessProfile), false);
                assetHasChanged = scope.changed;
            }

            if (GUI.Button(
                buttonNewRect, 
                EditorUtilities.GetContent("New|Create a new profile."), 
                showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
            {
                // By default, try to put assets in a folder next to the currently active
                // scene file. If the user isn't a scene, put them in root instead.
                var targetName = Target.name;
                var scene = Target.gameObject.scene;
                var asset = ProfileFactory.CreatePostProcessProfile(scene, targetName);
                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }

            if (showCopy && GUI.Button(
                buttonCopyRect, 
                EditorUtilities.GetContent("Clone|Create a new profile and copy the content of the currently assigned profile."), 
                EditorStyles.miniButtonRight))
            {
                // Duplicate the currently assigned profile and save it as a new profile
                var origin = (PostProcessProfile)m_Profile.objectReferenceValue;
                var path = AssetDatabase.GetAssetPath(origin);
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                var asset = Instantiate(origin);
                asset.settings.Clear();
                AssetDatabase.CreateAsset(asset, path);

                foreach (var item in origin.settings)
                {
                    var itemCopy = Instantiate(item);
                    itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    itemCopy.name = item.name;
                    asset.settings.Add(itemCopy);
                    AssetDatabase.AddObjectToAsset(itemCopy, asset);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged && m_EffectList != null)
                    m_EffectList.Clear(); // Asset wasn't null before, do some cleanup

                EditorGUILayout.HelpBox(
                    "Assign an existing Post-process Profile by choosing an asset, or create a new one by clicking the \"New\" button.\nNew assets are automatically put in a folder next to your scene file. If your scene hasn't been saved yet they will be created at the root of the Assets folder.", 
                    MessageType.Info);
            }
            else
            {
                if (assetHasChanged)
                    RefreshEffectListEditor((PostProcessProfile)m_Profile.objectReferenceValue);
                if (m_EffectList != null)
                    m_EffectList.OnGUI();
            }
        }
    }
} 
