using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering
{
    public static class VolumeProfileFactory
    {
        [MenuItem("Assets/Create/Volume Profile", priority = 201)]
        static void CreateVolumeProfile()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreatePostProcessProfile>(),
                "New Volume Profile.asset",
                null,
                null
                );
        }

        public static VolumeProfile CreateVolumeProfileAtPath(string path)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        public static VolumeProfile CreateVolumeProfile(Scene scene, string targetName)
        {
            string path;

            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name;
                var profilePath = scenePath + "/" + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                    AssetDatabase.CreateFolder(scenePath, extPath);

                path = profilePath + "/";
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        public static T CreateVolumeComponent<T>(VolumeProfile profile, bool overrides = false, bool saveAsset = true)
            where T : VolumeComponent
        {
            var comp = profile.Add<T>(overrides);
            AssetDatabase.AddObjectToAsset(comp, profile);

            if (saveAsset)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return comp;
        }
    }

    class DoCreatePostProcessProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}
