using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{

    public class DensityVolumeTextureTool : EditorWindow 
    {
        private Texture2D sourceTexture = null;
        private string assetPath;
        private string assetDirectory;

        private int tileSize = 0;

        private static GUIContent windowTitle = new GUIContent("Create Density Volume Texture");
        private static GUIContent textureLabel = new GUIContent("Slice Texture");
        private static GUIContent tileSizeLabel = new GUIContent("Texture Slice Size", "Dimensions of the created 3D Texture in pixels.  Width, Height and Depth are all the same size");
        private static GUIContent createLabel = new GUIContent("Create 3D Texture");

        [MenuItem("Window/Rendering/Density Volume Texture Tool")]
        static void Init()
        {
            DensityVolumeTextureTool window = (DensityVolumeTextureTool)EditorWindow.GetWindow(typeof(DensityVolumeTextureTool));
            window.titleContent = windowTitle;
            window.Show();
        }

        private bool IsTileSizeValid()
        {
            int textureWidth = sourceTexture.width;
            int textureHeight = sourceTexture.height;

            return (tileSize > 0 && (textureWidth * textureHeight >= (tileSize*tileSize*tileSize)));
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(textureLabel);
            EditorGUI.indentLevel++;
            sourceTexture = ( EditorGUILayout.ObjectField((UnityEngine.Object)sourceTexture, typeof(Texture2D), false, null) as Texture2D);
            EditorGUI.indentLevel--;
            tileSize = EditorGUILayout.IntField(tileSizeLabel, tileSize);

            bool validData = (sourceTexture != null && IsTileSizeValid());
            bool create = false;

            if (!validData)
            {
                if (tileSize > 0)
                {
                    EditorGUILayout.HelpBox(String.Format("Source Texture too small to generate a volume Texture of {0}x{0}x{0} size", tileSize), MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Tile size must be larger than 0", MessageType.Warning);
                }
            }

            using (new EditorGUI.DisabledScope(!validData))
            {
                create = GUILayout.Button(createLabel);
            }

            if (create)
            {
                assetPath = AssetDatabase.GetAssetPath(sourceTexture);
                assetDirectory = System.IO.Directory.GetParent(assetPath).ToString(); 

                //Check if the texture is set to read write.
                //Only need to do this since CopyTexture is currently broken on D3D11
                //So need to make sure there is a CPU copy ready to copy to the 3D Texture
                //The fix for this is coming soon.  Need to revist once it's in.
                TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                BuildVolumeTexture();
            }
        }

        private void BuildVolumeTexture()
        {
            //Check if the object we want to create is already in the AssetDatabase
            string volumeTextureAssetPath = assetDirectory + "/" + sourceTexture.name + "_Texture3D.asset";
            bool createNewAsset = false;

            Texture3D volumeTexture = AssetDatabase.LoadAssetAtPath(volumeTextureAssetPath, typeof(Texture3D)) as Texture3D;

            //If we already have the asset then we are just updating it. make sure it's the right size.
            if (!volumeTexture || volumeTexture.width != tileSize || volumeTexture.height != tileSize || volumeTexture.depth != tileSize) 
            {
                volumeTexture = new Texture3D(tileSize, tileSize, tileSize, sourceTexture.format, false);
                volumeTexture.filterMode = sourceTexture.filterMode;
                volumeTexture.mipMapBias = 0;
                volumeTexture.anisoLevel = 0;
                volumeTexture.wrapMode = sourceTexture.wrapMode;
                volumeTexture.wrapModeU = sourceTexture.wrapModeU;
                volumeTexture.wrapModeV = sourceTexture.wrapModeV;
                volumeTexture.wrapModeW = sourceTexture.wrapModeW;

                createNewAsset = true;
            }

            //Only need to do this since CopyTexture is currently broken on D3D11
            //Proper fix on it's way for 18.3 or 19.1
            Color [] colorArray = new Color[0];

            int yTiles = sourceTexture.height / tileSize;
            int xTiles = sourceTexture.width / tileSize;

            for (int i = yTiles - 1; i >= 0; i--)
            {
                for (int j = 0; j < xTiles; j++)
                {
                    Color [] sourceTile = sourceTexture.GetPixels(j * tileSize, i * tileSize, tileSize, tileSize);
                    Array.Resize(ref colorArray, colorArray.Length + sourceTile.Length);
                    Array.Copy(sourceTile, 0, colorArray, colorArray.Length - sourceTile.Length, sourceTile.Length);
                }
            }

            volumeTexture.SetPixels(colorArray);
            volumeTexture.Apply();

            if (createNewAsset)
            {
                AssetDatabase.CreateAsset(volumeTexture, volumeTextureAssetPath);
            }
            else
            {
                AssetDatabase.SaveAssets();
                //Asset can be currently used by Density Volume Manager so trigger refresh
                DensityVolumeManager.manager.TriggerVolumeAtlasRefresh();  
            }
        }
    }
}
