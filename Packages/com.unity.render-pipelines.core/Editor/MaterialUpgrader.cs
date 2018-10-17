using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Experimental.Rendering
{
    public static class DialogText
    {
        public static readonly string title = "Material Upgrader";
        public static readonly string proceed = "Proceed";
        public static readonly string ok = "Ok";
        public static readonly string cancel = "Cancel";
        public static readonly string noSelectionMessage = "You must select at least one material.";
        public static readonly string projectBackMessage = "Make sure to have a project backup before proceeding.";
    }

    public class MaterialUpgrader
    {
        public delegate void MaterialFinalizer(Material mat);

        string m_OldShader;
        string m_NewShader;

        MaterialFinalizer m_Finalizer;

        Dictionary<string, string> m_TextureRename = new Dictionary<string, string>();
        Dictionary<string, string> m_FloatRename = new Dictionary<string, string>();
        Dictionary<string, string> m_ColorRename = new Dictionary<string, string>();

        Dictionary<string, float> m_FloatPropertiesToSet = new Dictionary<string, float>();
        Dictionary<string, Color> m_ColorPropertiesToSet = new Dictionary<string, Color>();
        List<string> m_TexturesToRemove = new List<string>();
        Dictionary<string, Texture> m_TexturesToSet = new Dictionary<string, Texture>();


        class KeywordFloatRename
        {
            public string keyword;
            public string property;
            public float setVal, unsetVal;
        }
        List<KeywordFloatRename> m_KeywordFloatRename = new List<KeywordFloatRename>();

        [Flags]
        public enum UpgradeFlags
        {
            None = 0,
            LogErrorOnNonExistingProperty = 1,
            CleanupNonUpgradedProperties = 2,
            LogMessageWhenNoUpgraderFound = 4
        }

        public void Upgrade(Material material, UpgradeFlags flags)
        {
            Material newMaterial;
            if ((flags & UpgradeFlags.CleanupNonUpgradedProperties) != 0)
            {
                newMaterial = new Material(Shader.Find(m_NewShader));
            }
            else
            {
                newMaterial = UnityEngine.Object.Instantiate(material) as Material;
                newMaterial.shader = Shader.Find(m_NewShader);
            }

            Convert(material, newMaterial);

            material.shader = Shader.Find(m_NewShader);
            material.CopyPropertiesFromMaterial(newMaterial);
            UnityEngine.Object.DestroyImmediate(newMaterial);

            if (m_Finalizer != null)
                m_Finalizer(material);
        }

        // Overridable function to implement custom material upgrading functionality
        public virtual void Convert(Material srcMaterial, Material dstMaterial)
        {
            foreach (var t in m_TextureRename)
            {
                dstMaterial.SetTextureScale(t.Value, srcMaterial.GetTextureScale(t.Key));
                dstMaterial.SetTextureOffset(t.Value, srcMaterial.GetTextureOffset(t.Key));
                dstMaterial.SetTexture(t.Value, srcMaterial.GetTexture(t.Key));
            }

            foreach (var t in m_FloatRename)
                dstMaterial.SetFloat(t.Value, srcMaterial.GetFloat(t.Key));

            foreach (var t in m_ColorRename)
                dstMaterial.SetColor(t.Value, srcMaterial.GetColor(t.Key));

            foreach (var prop in m_TexturesToRemove)
                dstMaterial.SetTexture(prop, null);

            foreach (var prop in m_TexturesToSet)
                dstMaterial.SetTexture(prop.Key, prop.Value);

            foreach (var prop in m_FloatPropertiesToSet)
                dstMaterial.SetFloat(prop.Key, prop.Value);

            foreach (var prop in m_ColorPropertiesToSet)
                dstMaterial.SetColor(prop.Key, prop.Value);
            foreach (var t in m_KeywordFloatRename)
                dstMaterial.SetFloat(t.property, srcMaterial.IsKeywordEnabled(t.keyword) ? t.setVal : t.unsetVal);
        }

        public void RenameShader(string oldName, string newName, MaterialFinalizer finalizer = null)
        {
            m_OldShader = oldName;
            m_NewShader = newName;
            m_Finalizer = finalizer;
        }

        public void RenameTexture(string oldName, string newName)
        {
            m_TextureRename[oldName] = newName;
        }

        public void RenameFloat(string oldName, string newName)
        {
            m_FloatRename[oldName] = newName;
        }

        public void RenameColor(string oldName, string newName)
        {
            m_ColorRename[oldName] = newName;
        }

        public void RemoveTexture(string name)
        {
            m_TexturesToRemove.Add(name);
        }

        public void SetFloat(string propertyName, float value)
        {
            m_FloatPropertiesToSet[propertyName] = value;
        }

        public void SetColor(string propertyName, Color value)
        {
            m_ColorPropertiesToSet[propertyName] = value;
        }

        public void SetTexture(string propertyName, Texture value)
        {
            m_TexturesToSet[propertyName] = value;
        }

        public void RenameKeywordToFloat(string oldName, string newName, float setVal, float unsetVal)
        {
            m_KeywordFloatRename.Add(new KeywordFloatRename { keyword = oldName, property = newName, setVal = setVal, unsetVal = unsetVal });
        }

        static bool IsMaterialPath(string path)
        {
            return path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
        }

        static MaterialUpgrader GetUpgrader(List<MaterialUpgrader> upgraders, Material material)
        {
            if (material == null || material.shader == null)
                return null;

            string shaderName = material.shader.name;
            for (int i = 0; i != upgraders.Count; i++)
            {
                if (upgraders[i].m_OldShader == shaderName)
                    return upgraders[i];
            }

            return null;
        }

        //@TODO: Only do this when it exceeds memory consumption...
        static void SaveAssetsAndFreeMemory()
        {
            AssetDatabase.SaveAssets();
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }

        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            if (!EditorUtility.DisplayDialog(DialogText.title, "The upgrade will overwrite materials in your project. " + DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel))
                return;

            int totalMaterialCount = 0;
            foreach (string s in UnityEditor.AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(s))
                    totalMaterialCount++;
            }

            int materialIndex = 0;
            foreach (string path in UnityEditor.AssetDatabase.GetAllAssetPaths())
            {
                if (IsMaterialPath(path))
                {
                    materialIndex++;
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", materialIndex, totalMaterialCount, path), (float)materialIndex / (float)totalMaterialCount))
                        break;

                    Material m = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) as Material;
                    Upgrade(m, upgraders, flags);

                    //SaveAssetsAndFreeMemory();
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public static void Upgrade(Material material, MaterialUpgrader upgrader, UpgradeFlags flags)
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(upgrader);
            Upgrade(material, upgraders, flags);
        }

        public static void Upgrade(Material material, List<MaterialUpgrader> upgraders, UpgradeFlags flags)
        {
            if (material == null)
                return;

            var upgrader = GetUpgrader(upgraders, material);

            if (upgrader != null)
                upgrader.Upgrade(material, flags);
            else if ((flags & UpgradeFlags.LogMessageWhenNoUpgraderFound) == UpgradeFlags.LogMessageWhenNoUpgraderFound)
                Debug.Log(string.Format("{0} material was not upgraded. There's no upgrader to convert {1} shader to selected pipeline", material.name, material.shader.name));
        }

        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            var selection = Selection.objects;

            if (selection == null)
            {
                EditorUtility.DisplayDialog(DialogText.title, DialogText.noSelectionMessage, DialogText.ok);
                return;
            }

            List<Material> selectedMaterials = new List<Material>(selection.Length);
            for (int i = 0; i < selection.Length; ++i)
            {
                Material mat = selection[i] as Material;
                if (mat != null)
                    selectedMaterials.Add(mat);
            }

            int selectedMaterialsCount = selectedMaterials.Count;
            if (selectedMaterialsCount == 0)
            {
                EditorUtility.DisplayDialog(DialogText.title, DialogText.noSelectionMessage, DialogText.ok);
                return;
            }

            if (!EditorUtility.DisplayDialog(DialogText.title, string.Format("The upgrade will overwrite {0} selected material{1}. ", selectedMaterialsCount, selectedMaterialsCount > 1 ? "s" : "") +
                    DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel))
                return;

            string lastMaterialName = "";
            for (int i = 0; i < selectedMaterialsCount; i++)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressBarName, string.Format("({0} of {1}) {2}", i, selectedMaterialsCount, lastMaterialName), (float)i / (float)selectedMaterialsCount))
                    break;

                var material = selectedMaterials[i];
                Upgrade(material, upgraders, flags);
                if (material != null)
                    lastMaterialName = material.name;
            }

            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}
