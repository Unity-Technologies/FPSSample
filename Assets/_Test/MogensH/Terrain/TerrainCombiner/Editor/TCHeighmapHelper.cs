using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR

namespace PocketHammer
{

	public class TCHeightmapHelper {

		public static void UpdateCombinerCache(TerrainCombiner combiner)
		{
			TerrainData terrainData = combiner.GetComponent<Terrain>().terrainData;

			// TODO: release ??

			int targetSize = terrainData.heightmapResolution;
			combiner.HeightmapCache.Texture = TCGraphicsHelper.CreateTexture(targetSize);
			combiner.HeightmapCache.ResultData = new float[targetSize,targetSize];
			combiner.HeightmapCache.RenderTarget = TCGraphicsHelper.CreateRenderTarget(targetSize);
		}


		public static void UpdateSourceCache(TerrainCombinerSource source)
		{
			Terrain targetTerrain = source.GetComponent<Terrain>();
			TerrainData targetTerrainData = targetTerrain.terrainData;

			// TODO: reuse/scale textures ?
			// TODO: release?

			int size = targetTerrainData.heightmapResolution;
			source.CachedHeightmapTexture = TCGraphicsHelper.CreateTexture(size);
			TCGraphicsHelper.LoadTextureData(targetTerrainData.GetHeights(0,0,size,size), ref source.CachedHeightmapTexture); 
		}

		public static void StartCombine(TerrainCombiner terrainCombiner, float groundLevelFraction)
		{
			Terrain targetTerrain = terrainCombiner.GetComponent<Terrain>();
//			TerrainData targetTerrainData = targetTerrain.terrainData;

			RenderTexture prevRenderTarget = RenderTexture.active;
			Graphics.SetRenderTarget(terrainCombiner.HeightmapCache.RenderTarget);
			GL.Clear(false,true,new Color(groundLevelFraction,0,0,1));
	
			{
				for(int i=0;i<terrainCombiner.Instances.Length;i++) {
					TerrainCombinerInstance terrainInstance = terrainCombiner.Instances[i];

					if(terrainInstance.source == null) {
						continue;
					}

					Terrain sourceTerrain = terrainInstance.source.GetComponent<Terrain>();
					if(sourceTerrain == null) {
						continue;
					}

//					TerrainData sourceTerrainData = sourceTerrain.terrainData;

//					Texture2D sourceTexture = heightmapDataCache.sourceTextures[i];
					Texture2D sourceTexture = terrainInstance.source.CachedHeightmapTexture;


					Vector2 position = terrainInstance.position;

					//			Material material = new Material(Shader.Find("Unlit/Texture"));
					Material material = new Material(Shader.Find("PockerHammer/TCHeightmapShader"));
					material.SetFloat("_HeighOffset",-terrainInstance.source.GroundLevelFraction);

					float heightScale = terrainInstance.WorldSize.y/terrainCombiner.WorldSize.y;
					material.SetFloat("_HeightScale",heightScale);

					Vector2 scale = TerrainCombiner.CalcChildTerrainPlaneScale(targetTerrain.terrainData.size, sourceTerrain.terrainData.size, terrainInstance.size);

					TCGraphicsHelper.DrawTexture(sourceTexture, material, position, terrainInstance.rotation, scale);
				}
			}

			Graphics.SetRenderTarget(prevRenderTarget);
		}



		public static void SampleTexture(TerrainCombiner terrainCombiner, int size) {

			RenderTexture prevRenderTarget = RenderTexture.active;
			Graphics.SetRenderTarget(terrainCombiner.HeightmapCache.RenderTarget);

			TCGraphicsHelper.ReadRenderTarget(size, ref terrainCombiner.HeightmapCache.Texture);
			TCGraphicsHelper.ReadDataFromTexture(terrainCombiner.HeightmapCache.Texture, ref terrainCombiner.HeightmapCache.ResultData);

			Graphics.SetRenderTarget(prevRenderTarget);
		}

		public static void ApplyData(TerrainCombiner terrainCombiner) {
			Terrain targetTerrain = terrainCombiner.GetComponent<Terrain>();
			TerrainData targetTerrainData = targetTerrain.terrainData;
			targetTerrainData.SetHeights(0,0,terrainCombiner.HeightmapCache.ResultData);
		}
	}
}

#endif