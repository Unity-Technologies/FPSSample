using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class EditorReflectionSystem
    {
        const HideFlags k_ReflectionSystemDictionaryHideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.DontSaveInBuild;

        public static bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        public static bool IsCollidingWithOtherProbes(string targetPath, PlanarReflectionProbe targetProbe, out PlanarReflectionProbe collidingProbe)
        {
            PlanarReflectionProbe[] probes = Object.FindObjectsOfType<PlanarReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customTexture == null)
                    continue;
                var path = AssetDatabase.GetAssetPath(probe.customTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        public static void BakeCustomReflectionProbe(PlanarReflectionProbe probe, bool usePreviousAssetPath)
        {
            string path;
            if (!GetCustomBakePath(probe.name, probe.customTexture, true, usePreviousAssetPath, out path))
                return;

            PlanarReflectionProbe collidingProbe;
            if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
            {
                if (!EditorUtility.DisplayDialog("Texture is used by other reflection probe",
                        string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                            path, collidingProbe.name), "Yes", "No"))
                {
                    return;
                }
            }

            EditorUtility.DisplayProgressBar("Planar Reflection Probes", "Baking " + path, 0.5f);
            if (!BakePlanarReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();

            AssetDatabase.ImportAsset(path);
            probe.customTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void BakeAllPlanarReflectionProbes()
        {
            var probes = Object.FindObjectsOfType<PlanarReflectionProbe>();
            for (var i = 0; i < probes.Length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Baking Planar Probes",
                    string.Format("Probe {0} / {1}", i + 1, probes.Length),
                    (i + 1) / (float)probes.Length);

                var probe = probes[i];
                var bakePath = GetBakePathFor(probe);
                var bakePathInfo = new FileInfo(bakePath);
                if (!bakePathInfo.Directory.Exists)
                    bakePathInfo.Directory.Create();
                BakePlanarReflectionProbe(probe, bakePath);
            }
        }

        static string GetBakePathFor(PlanarReflectionProbe probe)
        {
            var scene = probe.gameObject.scene;
            var directory = Path.Combine(Path.GetDirectoryName(scene.path), Path.GetFileNameWithoutExtension(scene.path));
            var filename = string.Format("PlanarReflectionProbe-{0}.exr", 0);

            return Path.Combine(directory, filename);
        }

        public static bool BakePlanarReflectionProbe(PlanarReflectionProbe probe, string path)
        {
            var rt = ReflectionSystem.NewRenderTarget(probe);
            ReflectionSystem.Render(probe, rt);
            var target = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false, true);
            var a = RenderTexture.active;
            RenderTexture.active = rt;
            target.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = a;
            rt.Release();

            if (!WriteAndImportTexture(path, target))
                return false;

            return true;
        }

        public static void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath)
        {
            string path;
            if (!GetCustomBakePath(probe.name, probe.customBakedTexture, probe.hdr, usePreviousAssetPath, out path))
                return;

            ReflectionProbe collidingProbe;
            if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
            {
                if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                        string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                            path, collidingProbe.name), "Yes", "No"))
                {
                    return;
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!UnityEditor.Lightmapping.BakeReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }

        static MethodInfo k_Lightmapping_BakeReflectionProbeSnapshot = typeof(UnityEditor.Lightmapping).GetMethod("BakeReflectionProbeSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeReflectionProbeSnapshot(ReflectionProbe probe)
        {
            return (bool)k_Lightmapping_BakeReflectionProbeSnapshot.Invoke(null, new object[] { probe });
        }

        public static bool BakeReflectionProbeSnapshot(PlanarReflectionProbe probe)
        {
            var rt = ReflectionSystem.NewRenderTarget(probe);
            var bakedTexture = probe.bakedTexture as Texture2D;
            var assetPath = string.Empty;
            if (bakedTexture != null)
                assetPath = AssetDatabase.GetAssetPath(bakedTexture);
            if (string.IsNullOrEmpty(assetPath))
                assetPath = GetBakePath(probe);

            if (bakedTexture == null || string.IsNullOrEmpty(assetPath))
            {
                bakedTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, true, false);
                probe.bakedTexture = bakedTexture;
            }

            ReflectionSystem.Render(probe, rt);

            var art = RenderTexture.active;
            RenderTexture.active = rt;
            bakedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = art;

            WriteAndImportTexture(assetPath, bakedTexture);

            return true;
        }

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(UnityEditor.Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeAllReflectionProbesSnapshots()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, new object[0]);
        }

        static bool GetCustomBakePath(string probeName, Texture customBakedTexture, bool hdr, bool usePreviousAssetPath, out string path)
        {
            path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(customBakedTexture);

            var targetExtension = hdr ? "exr" : "png";
            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + targetExtension)
            {
                // We use the path of the active scene as the target path
                var targetPath = GetSceneBakeDirectoryPath(SceneManager.GetActiveScene());
                if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                var fileName = probeName + (hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return false;
            }
            return true;
        }

        static string GetBakePath(PlanarReflectionProbe probe)
        {
            var id = GetProbePersistentID(probe);
            if (id == -1)
                return string.Empty;

            var scene = probe.gameObject.scene;

            var targetPath = GetSceneBakeDirectoryPath(scene);
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            var fileName = probe.name + "-reflectionHDR.exr";
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName));
        }

        static string GetSceneBakeDirectoryPath(Scene scene)
        {
            var targetPath = scene.path;
            targetPath = Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileNameWithoutExtension(targetPath));
            if (string.IsNullOrEmpty(targetPath))
                targetPath = "Assets";
            else if (Directory.Exists(targetPath) == false)
                Directory.CreateDirectory(targetPath);
            return targetPath;
        }

        static int GetProbePersistentID(PlanarReflectionProbe probe)
        {
            var scene = probe.gameObject.scene;
            if (!scene.IsValid())
                return -1;

            var reflectionDictionary = GetReflectionDictionaryFor(scene);
            return reflectionDictionary.GetIdFor(probe);
        }

        static ReflectionSystemSceneDictionary GetReflectionDictionaryFor(Scene scene)
        {
            ReflectionSystemSceneDictionary result = null;

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (var i = 0; i < roots.Count; i++)
            {
                result = roots[i].GetComponent<ReflectionSystemSceneDictionary>();
                if (result != null)
                    break;
            }

            if (result == null)
            {
                result = EditorUtility.CreateGameObjectWithHideFlags(
                        "Reflection System Dictionary",
                        k_ReflectionSystemDictionaryHideFlags,
                        typeof(ReflectionSystemSceneDictionary))
                    .GetComponent<ReflectionSystemSceneDictionary>();

                SceneManager.MoveGameObjectToScene(result.gameObject, scene);
                result.gameObject.SetActive(false);
            }

            result.gameObject.hideFlags = k_ReflectionSystemDictionaryHideFlags;

            return result;
        }

        static bool WriteAndImportTexture(string path, Texture2D target)
        {
            var bytes = target.EncodeToEXR();
            try
            {
                var targetFile = new FileInfo(path);
                if (!targetFile.Directory.Exists)
                    targetFile.Directory.Create();
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;

                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    importer.textureCompression = hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed
                        ? TextureImporterCompression.Compressed
                        : TextureImporterCompression.Uncompressed;
                }

                importer.SaveAndReimport();
            }
            return true;
        }
    }
}
