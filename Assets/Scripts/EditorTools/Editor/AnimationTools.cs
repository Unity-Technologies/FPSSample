using UnityEngine;
using UnityEditor;

public class AnimTools : EditorWindow
{
    public float hSliderValue = 1.0F;


    /// <summary>
    /// Update all animation avatar masks, if they are set to copy another avatar mask and this mask has changed.
    /// </summary>
    /// <remarks>
    /// This is to stop the user from having to update the masks of all dependant animation clips everytime a "Source" mask changes.
    /// Fall out of not having resource dependencies, but needing them anyway.
    /// </remarks>
    [MenuItem("FPS Sample/Animation/Update Animation Masks")]
    static void UpdateAnimationMasks()
    {
        var guids = AssetDatabase.FindAssets("t:animation");

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;

            if (modelImporter == null)
                continue;

            var clips = modelImporter.clipAnimations;
            var clipSettingUpdated = false;

            foreach (var clip in clips)
            {
                if (clip.maskType != ClipAnimationMaskType.CopyFromOther)
                    continue;

                if (clip.maskNeedsUpdating)
                {
                    Debug.Log("Updating mask for clip: " + clip.name);
                    clip.ConfigureClipFromMask(clip.maskSource);
                    clipSettingUpdated = true;
                }
            }

            if (clipSettingUpdated)
            {
                Debug.Log("Clip Settings Updated!");
                modelImporter.clipAnimations = clips;
                modelImporter.SaveAndReimport();
            }
        }
    }

    /// <summary>
    /// Update all animation avatar definitions, if they are set to copy another avatar definition.
    /// </summary>
    /// <remarks>
    /// This is to stop the user from having to update the avatar definition of all dependant avatars everytime a "Source" definition changes.
    /// Fall out of not having resource dependencies, but needing them anyway.
    /// </remarks>
    [MenuItem("FPS Sample/Animation/Update Avatar References")]
    static void UpdateAvatarReferences()
    {
        var guids = AssetDatabase.FindAssets("t:model");

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;

            if (modelImporter == null)
                continue;

            if (modelImporter.sourceAvatar)
            {
                Debug.Log("Updating source avatar for model: " + assetPath);
                modelImporter.sourceAvatar = modelImporter.sourceAvatar;
                modelImporter.SaveAndReimport();
            }
        }
    }


}
