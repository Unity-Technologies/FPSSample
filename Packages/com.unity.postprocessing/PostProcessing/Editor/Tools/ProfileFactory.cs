using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public class ProfileFactory
    {
        [MenuItem("Assets/Create/Post-processing Profile", priority = 201)]
        static void CreatePostProcessProfile()
        {
            //var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreatePostProcessProfile>(), "New Post-processing Profile.asset", null, null);
        }

        public static PostProcessProfile CreatePostProcessProfileAtPath(string path)
        {
            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            profile.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        public static PostProcessProfile CreatePostProcessProfile(Scene scene, string targetName)
        {
            var path = string.Empty;

            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name + "_Profiles";
                var profilePath = scenePath + "/" + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                    AssetDatabase.CreateFolder(scenePath, extPath);

                path = profilePath + "/";
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
                        
            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }
    }

    class DoCreatePostProcessProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = ProfileFactory.CreatePostProcessProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}
