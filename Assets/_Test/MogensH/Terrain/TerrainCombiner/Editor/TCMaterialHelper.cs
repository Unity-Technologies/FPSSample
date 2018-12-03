#if UNITY_EDITOR

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace PocketHammer
{

	public class TCMaterialHelper {

		public static void UpdateCombinerCache(TerrainCombiner combiner)
		{
			TerrainData terrainData = combiner.GetComponent<Terrain>().terrainData;

			// Update target materials from sources
			List<TerrainLayer> targetMatList = new List<TerrainLayer>();
			for(int i=0;i<combiner.Instances.Length; i++) {
				TerrainCombinerInstance sourceData = combiner.Instances[i];

				if(sourceData.source == null) {
					continue;
				}

				Terrain sourceTerrain = sourceData.source.GetComponent<Terrain>();
				foreach(var terrainLayer in sourceTerrain.terrainData.terrainLayers) {

                    if (terrainLayer.diffuseTexture == null)
                        continue;

                    // If splay use alpha material it should not be added to target
					if(sourceData.source.alphaMaterial != null && terrainLayer == sourceData.source.alphaMaterial) {
						continue;
					}

					int index = TCMaterialHelper.GetLayerIndex(targetMatList.ToArray(), terrainLayer);
					if(index == -1) {
						targetMatList.Add(terrainLayer);
					}
				}
			}
			terrainData.terrainLayers = targetMatList.ToArray();


			// TODO: release ??
			combiner.MaterialCache.RenderTextures.Clear();

			int size = terrainData.alphamapResolution;
			int targetLayerCount = terrainData.alphamapLayers;
			for(int i=0;i<targetLayerCount;i++) {
				RenderTexture renderTexture = TCGraphicsHelper.CreateRenderTarget(size);
				combiner.MaterialCache.RenderTextures.Add(renderTexture);
			}

			combiner.MaterialCache.Texture = TCGraphicsHelper.CreateTexture(size);
			combiner.MaterialCache.ResultData = new float[size,size,terrainData.alphamapLayers];
		}

		public static void UpdateSourceCache(TerrainCombinerSource source)
		{
			TerrainData sourceTerrainData = source.GetComponent<Terrain>().terrainData;

			// TODO: reuse/scale textures ?
			// TODO: release?

			source.CachedMaterials.Clear();

			int size = sourceTerrainData.alphamapResolution;
			float[,,] alphaMaps = sourceTerrainData.GetAlphamaps(0,0,size,size);
			for(int layer=0;layer<sourceTerrainData.alphamapLayers;layer++) {
				Texture2D texture = TCGraphicsHelper.CreateTexture(size);
				TCGraphicsHelper.LoadTextureData(alphaMaps, layer, ref texture);
				source.CachedMaterials.Add(texture);
			}
		}

		// TODO: find better place
		private static Texture2D CreateTexture(Color color) {
			var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

			// set the pixel values
			texture.SetPixel(0, 0, color);
			texture.SetPixel(1, 0, color);
			texture.SetPixel(0, 1, color);
			texture.SetPixel(1, 1, color);

			// Apply all SetPixel calls
			texture.Apply();

			return texture;
		}


		public static void StartCombine(TerrainCombiner terrainCombiner)
		{
			Terrain targetTerrain = terrainCombiner.GetComponent<Terrain>();
			TerrainData targetTerrainData = targetTerrain.terrainData;

			RenderTexture prevRenderTarget = RenderTexture.active;

			Texture2D blackTexture = CreateTexture(Color.black); // TODO: cache?


			// Iterate through alphamaps 
			for(int nLayer=0;nLayer<targetTerrainData.alphamapLayers;nLayer++) {
				Graphics.SetRenderTarget(terrainCombiner.MaterialCache.RenderTextures[nLayer]);
				GL.Clear(false,true,Color.black);
			
				var terrainLayer = targetTerrainData.terrainLayers[nLayer];

				// Apply all sources
				for(int i=0;i<terrainCombiner.Instances.Length; i++) {
					TerrainCombinerInstance sourceData = terrainCombiner.Instances[i];

					if(sourceData.source == null) {
						continue;
					}

					Terrain sourceTerrain = sourceData.source.GetComponent<Terrain>();
					TerrainData sourceTerrainData = sourceTerrain.terrainData;

						//					// TODO: map splat prototypes to find correct id
						//					int nTexture = nLayer/4;
						//					int nColor = nLayer % 4;
						//
						//					Texture2D[] textures = sourceTerrainData.alphamapTextures;
						//
						//					if(textures != null && textures.Length > nTexture) {
						//
						//						Vector2 position = sourceData.position;
						//						Texture2D sourceTexture = textures[nTexture];
						//
						//						Material material = new Material(Shader.Find("PockerHammer/TCMaterialShader"));
						//						Matrix4x4 m = Matrix4x4.zero;
						//
						//						switch(nColor) {
						//						case 0:
						//							m.m00 = 1.0f;
						//							break;
						//						case 1:
						//							m.m11 = 1.0f;
						//							break;
						//						case 2:
						//							m.m22 = 1.0f;
						//							break;
						//						case 3:
						//							m.m33 = 1.0f;
						//							break;
						//						}
						//
						//						material.SetMatrix ("_ColorMapping", m);
						//
						//						TCGraphicsHelper.DrawTexture(sourceTexture, material, position, sourceData.rotation, sourceData.scale);
						//
						//					}

						//				if(nSourceLayer < sourceData.source.CachedMaterialTextures.Count) {


					int nSourceLayer = GetLayerIndex(sourceTerrainData.terrainLayers, terrainLayer);
					Texture2D sourceTexture = nSourceLayer != -1 ?sourceData.source.CachedMaterials[nSourceLayer] : blackTexture;

					Texture2D alphaTexture = blackTexture;
					if(sourceData.source.alphaMaterial != null) {
						
						int nSourceAlphaLayer = GetLayerIndex(sourceTerrainData.terrainLayers, sourceData.source.alphaMaterial );
						if (nSourceAlphaLayer == -1)
							continue;
						alphaTexture = sourceData.source.CachedMaterials[nSourceAlphaLayer];
					}

					Material material = new Material(Shader.Find("PockerHammer/TCMaterialShader"));
					material.SetTexture("_Texture2",alphaTexture);

					Vector2 scale = TerrainCombiner.CalcChildTerrainPlaneScale(targetTerrain.terrainData.size, sourceTerrain.terrainData.size, sourceData.size);

					TCGraphicsHelper.DrawTexture(sourceTexture, material, sourceData.position, sourceData.rotation, scale);
				}
			}
			Graphics.SetRenderTarget(prevRenderTarget);
		}


		public static void SampleTexture(TerrainCombiner terrainCombiner) {
			Terrain targetTerrain = terrainCombiner.GetComponent<Terrain>();
			TerrainData targetTerrainData = targetTerrain.terrainData;

			RenderTexture prevRenderTarget = RenderTexture.active;
		
			for(int nLayer=0;nLayer<targetTerrainData.alphamapLayers;nLayer++) {

				Graphics.SetRenderTarget(terrainCombiner.MaterialCache.RenderTextures[nLayer]);

				// Sample render target
				int targetSize = targetTerrainData.alphamapResolution;
				TCGraphicsHelper.ReadRenderTarget(targetSize, ref terrainCombiner.MaterialCache.Texture);
				TCGraphicsHelper.ReadDataFromTexture(terrainCombiner.MaterialCache.Texture, ref terrainCombiner.MaterialCache.ResultData, nLayer);
			}

			Graphics.SetRenderTarget(prevRenderTarget);	
		}


		public static void SampleTexture(TerrainCombiner terrainCombiner, int nLayer) {
			Terrain targetTerrain = terrainCombiner.GetComponent<Terrain>();
			TerrainData targetTerrainData = targetTerrain.terrainData;

			RenderTexture prevRenderTarget = RenderTexture.active;

			Graphics.SetRenderTarget(terrainCombiner.MaterialCache.RenderTextures[nLayer]);

			// Sample render target
			int targetSize = targetTerrainData.alphamapResolution;
			TCGraphicsHelper.ReadRenderTarget(targetSize, ref terrainCombiner.MaterialCache.Texture);
			TCGraphicsHelper.ReadDataFromTexture(terrainCombiner.MaterialCache.Texture, ref terrainCombiner.MaterialCache.ResultData, nLayer);

			Graphics.SetRenderTarget(prevRenderTarget);	
		}
	

		public static void ApplyData(TerrainCombiner terrainCombiner) {
			Terrain targetTerrain = terrainCombiner.GetComponent<Terrain>();
			TerrainData targetTerrainData = targetTerrain.terrainData;
			targetTerrainData.SetAlphamaps(0,0,terrainCombiner.MaterialCache.ResultData);
		}

		public static int GetLayerIndex(TerrainLayer[] layers, TerrainLayer layer)
		{
			var layerPath = AssetDatabase.GetAssetPath(layer);
			for (int i = 0; i < layers.Length; i++)
			{
				var entryPath = AssetDatabase.GetAssetPath(layers[i]);
				if (layerPath == entryPath)
					return i;
			}

			return -1;
		}
		
		public static void AddTerrainLayer(TerrainData data, TerrainLayer layer)
		{
			var index = GetLayerIndex(data.terrainLayers, layer);
			if (index > -1)
				return;
			var list = new List<TerrainLayer>(data.terrainLayers);
			list.Add(layer);
			data.terrainLayers = list.ToArray();
		}
	}

}

#endif