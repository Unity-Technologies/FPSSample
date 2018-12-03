using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR

namespace PocketHammer
{
	public class TerrainSettingsHelper {


		static public void AdjustTransform(Terrain terrain, Vector3 delta, bool updateTerrain, bool updateChildren) {

			if(updateTerrain) {
				terrain.transform.position += delta;
			}

			if(updateChildren) {
				for(int i=0;i<terrain.transform.childCount;i++) {
					Transform child = terrain.transform.GetChild(i);
					child.position -= delta;
				}
			}
		}

		static private void PrepareTextureRFloat(ref Texture2D texture, int size) {
			if(texture == null) {
				texture = new Texture2D(size,size,TextureFormat.RGBAFloat, false);
			}
			else 
			{
				texture.Resize(size,size);
			}
		}

		public static void ScaleTexture(Texture2D tex, int size, FilterMode mode = FilterMode.Trilinear)
		{
			Rect texR = new Rect(0,0,size,size);
			DrawTexture(tex,size,mode);

			tex.Resize(size, size);
			tex.ReadPixels(texR,0,0,true);
			tex.Apply(true); 
		}

		static void DrawTexture(Texture2D src, int size, FilterMode fmode)
		{
			// Apply texture
			src.filterMode = fmode;
			src.Apply(true);       

			// Create rendertexture abd set as target
			RenderTexture rtt = new RenderTexture(size, size, 0,RenderTextureFormat.ARGBFloat);
			Graphics.SetRenderTarget(rtt);

			// Setup scale and material 
			GL.LoadPixelMatrix(0,1,1,0);
			Material material = new Material(Shader.Find("Unlit/Texture"));

			// Draw
			Graphics.DrawTexture(new Rect(0,0,1,1),src, material);
		}



		//  HEIGHT
		static private void LoadTextureFromHeightData(TerrainData terrainData, ref Texture2D texture) {

			int size = terrainData.heightmapResolution;
			PrepareTextureRFloat(ref texture, size);

			float[,] heights = terrainData.GetHeights(0,0,size,size);
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					float height = heights[x,y];
					Color color = new Color(height,0.0f,0.0f,1.0f);
					texture.SetPixel(x,y,color);
				}
			}
			texture.Apply();
		}



		private static void ReadHeightDataFromTexture(Texture2D texture, ref TerrainData terrainData) {

			int size = terrainData.heightmapResolution;

			float[,] newHeights = new float[size,size];
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					Color color = texture.GetPixel(x,y);
					float height = color.r;
					newHeights[x,y] = height;
				}
			}
			terrainData.SetHeights(0,0,newHeights);
		}



		public static void HeightDataDouble(TerrainData terrainData, ref Texture2D texture) {

			LoadTextureFromHeightData(terrainData, ref texture);

			int prevSize = terrainData.heightmapResolution;
			int newSize = (prevSize - 1)*2 + 1;

			ScaleTexture(texture, newSize);

			terrainData.heightmapResolution = newSize;

			ReadHeightDataFromTexture(texture, ref terrainData);

			// halve size as setting heightmap resolution adjusts size
			Vector3 size = terrainData.size;
			size.x *= 0.5f;
			size.z *= 0.5f;
			terrainData.size = size;
		}

		public static void HeightDataHalve(TerrainData terrainData, ref Texture2D texture) {

			LoadTextureFromHeightData(terrainData, ref texture);

			int prevSize = terrainData.heightmapResolution;
			int newSize = (prevSize - 1)/2 + 1;

			ScaleTexture(texture, newSize);

			terrainData.heightmapResolution = newSize;

			ReadHeightDataFromTexture(texture, ref terrainData);

			// halve size as setting heightmap resolution adjusts size
			Vector3 size = terrainData.size;
			size.x *= 2.0f;
			size.z *= 2.0f;
			terrainData.size = size;
		}

		// Material

		static private void LoadTextureFromAlphaMaps(float[,,] alphaMaps, int layer, ref Texture2D texture) {
			int size = alphaMaps.GetLength(0);
			PrepareTextureRFloat(ref texture, size);
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					float val = alphaMaps[x,y,layer];
					Color color = new Color(val,0.0f,0.0f,1.0f);
					texture.SetPixel(x,y,color);
				}
			}
			texture.Apply();
		}

//		private static void ReadAlphaMapFromTexture(Texture2D texture, float[,,] alphaMaps, int layer) {
//
//			int size = alphaMaps.GetLength(0);
//			float fraction = 1.0f/(float)(size);
//			for(int y=0;y<size;y++) {
//				float v = y*fraction;
//				for(int x=0;x<size;x++) {
//					float u = x*fraction;
//					Color color = texture.GetPixelBilinear(u,v);
//					float value = color.r;
//					alphaMaps[x,y,layer] = value;
//				}
//			}
//		}

		private static void ReadAlphaMapFromTexture(Texture2D texture, float[,,] alphaMaps, int layer) {

			int size = alphaMaps.GetLength(0);
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					Color color = texture.GetPixel(x,y);
					float value = color.r;
					alphaMaps[x,y,layer] = value;
				}
			}
		}


		public static void ChangeAlphaMapResolution(TerrainData terrainData, int newSize) {

			int prevSize = terrainData.alphamapResolution;

			// Read original alpha maps
			float[,,] prevAlphaMap = terrainData.GetAlphamaps(0,0,prevSize,prevSize);

			// Change resolution
			terrainData.alphamapResolution = newSize;
			float[,,] newAlphaMap = new float[newSize,newSize,terrainData.alphamapLayers];

			// 
			Texture2D texture = null;
			for(int layer=0;layer<terrainData.alphamapLayers;layer++) {
				LoadTextureFromAlphaMaps(prevAlphaMap, layer, ref texture);

				ScaleTexture(texture,newSize);

				ReadAlphaMapFromTexture(texture, newAlphaMap, layer);
			}

			terrainData.SetAlphamaps(0,0,newAlphaMap);
		}

		public static void DoubleAlphaMapResolution(TerrainData terrainData) {

			int prevSize = terrainData.alphamapResolution;
			int newSize = prevSize*2;

			// Read original alpha maps
			float[,,] prevAlphaMap = terrainData.GetAlphamaps(0,0,prevSize,prevSize);

			// Change resolution
			terrainData.alphamapResolution = newSize;
			float[,,] newAlphaMap = new float[newSize,newSize,terrainData.alphamapLayers];
		
			// 
			Texture2D texture = null;
			for(int layer=0;layer<terrainData.alphamapLayers;layer++) {
				LoadTextureFromAlphaMaps(prevAlphaMap, layer, ref texture);

				ReadAlphaMapFromTexture(texture, newAlphaMap, layer);
			}

			terrainData.SetAlphamaps(0,0,newAlphaMap);
		}

		public static bool AdjustCeiling(TerrainData terrainData, float deltaWorldUnits)
		{
			float scale = 1.0f + (deltaWorldUnits/terrainData.size.y);
			float dataScale = 1.0f / scale;

			// Find min and max height
			float heightMin;
			float heightMax;
			FindHeightMinMax(terrainData, out heightMin, out heightMax);

			// Ensure scaled value dont become to large
			if(heightMax*dataScale > 1.0f) {
				return false;
			}

			// Adjust trees
			float treeHeightOffset = deltaWorldUnits/terrainData.size.y;
			TreeInstance[] instances = terrainData.treeInstances;
			for (int i=0;i< instances.Length;i++)
			{
				Vector3 pos = instances[i].position;
				pos.y -= treeHeightOffset;
				instances[i].position = pos;
			}
			terrainData.treeInstances = instances;
			terrainData.RefreshPrototypes();

			// Adjust terrain height
			Vector3 size = terrainData.size;
			size.y += deltaWorldUnits;
			terrainData.size = size;

			// Adjust data to keep proprotions
			ScaleDataHeight(terrainData, dataScale);




			return true;
		}

		public static bool AdjustFloor(TerrainData terrainData, float deltaWorldUnits)
		{
			float scale = 1.0f + (-deltaWorldUnits/terrainData.size.y);
			float dataScale = 1.0f / scale;

			// Find min and max height
			float heightMin;
			float heightMax;
			FindHeightMinMax(terrainData, out heightMin, out heightMax);

			// Ensure scaled value dont become to large
			float dataOffset = 1.0f - dataScale;

			if(dataOffset < -heightMin) {
				return false;
			}

			// Adjust terrain height
			Vector3 size = terrainData.size;
			size.y += -deltaWorldUnits;
			terrainData.size = size;

			// Adjust data to keep proprotions
			ScaleDataHeight(terrainData, dataScale);

			OffsetDataHeight(terrainData, dataOffset);

			return true;
		}



		private static void FindHeightMinMax(TerrainData terrainData, out float min, out float max) {

			float[,] heights = terrainData.GetHeights(0,0,terrainData.heightmapResolution,terrainData.heightmapResolution);
			min = 1.0f;
			max = 0.0f;
			for (int y = 0; y < terrainData.heightmapResolution; y++) {
				for (int x = 0; x < terrainData.heightmapResolution; x++) {
					float height = heights[x,y];
					min = Mathf.Min(min, height);
					max = Mathf.Max(max, height);
				}
			}
		}

		private static void ScaleDataHeight(TerrainData terrainData, float scale)
		{
			float[,] heights = terrainData.GetHeights(0,0,terrainData.heightmapResolution,terrainData.heightmapResolution);
			for (int y = 0; y < terrainData.heightmapResolution; y++) {
				for (int x = 0; x < terrainData.heightmapResolution; x++) {
					float height = heights[x,y];
					heights[x,y] = height*scale;
				}
			}
			terrainData.SetHeights(0,0,heights);
		}

		private static void OffsetDataHeight(TerrainData terrainData, float offset)
		{
			float[,] heights = terrainData.GetHeights(0,0,terrainData.heightmapResolution,terrainData.heightmapResolution);
			for (int y = 0; y < terrainData.heightmapResolution; y++) {
				for (int x = 0; x < terrainData.heightmapResolution; x++) {
					float height = heights[x,y];
					heights[x,y] = height + offset;
				}
			}
			terrainData.SetHeights(0,0,heights);
		}

		private static void AddHeightToHistogram(float height, ref Dictionary<float,int> histogram) {
			int count = 1;
			if(histogram.TryGetValue(height, out count)) {
				count ++;
			}
			histogram[height] = count;
		}

		private static float[,,] CreateAlphaMaps(int resolution, int layerCount, int defaultLayer) {

			float[,,] newMaps = new float[resolution, resolution, layerCount];

			// Clear new map to only contain default material
			for (int layer = 0; layer < layerCount; layer++)
			{
				int val = layer == defaultLayer ? 1 : 0;
				for (int y = 0; y < resolution; y++)
				{
					for (int x = 0; x < resolution; x++)
					{
						newMaps[x, y, layer] = val;
					}
				}
			}

			return newMaps;
		}

		// Get histogram of edge height
		private static void GetEdgeHeightHistogram(TerrainData terrainData, ref Dictionary<float,int> histogram) {
			int size = terrainData.heightmapResolution;
			float[,] heights = terrainData.GetHeights(0,0,size,size);

			int maxIndex = size - 1;
			for(int i=0;i<size;i++) {

				AddHeightToHistogram(heights[0, i], ref histogram);
				AddHeightToHistogram(heights[maxIndex, i], ref histogram);
				AddHeightToHistogram(heights[i, 0], ref histogram);
				AddHeightToHistogram(heights[i, maxIndex], ref histogram);
			}
		}
			
		public static void GetHeightDataBounds(TerrainData terrainData, float refHeight, ref Rect bounds) {

			int minX = terrainData.heightmapResolution;
			int minY = minX;
			int maxX = 0;
			int maxY = 0;

			int size = terrainData.heightmapResolution;
			float[,] heights = terrainData.GetHeights(0,0,size,size);

			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					float height = heights[x,y];
					if(height != refHeight) {
						minX = Mathf.Min(x,minX);
						minY = Mathf.Min(y,minY);
						maxX = Mathf.Max(x,maxX);
						maxY = Mathf.Max(y,maxY);
					}
				}
			}

			bounds.Set(minX,minY,maxX - minX, maxY - minY);
		}


		public static void MoveData(TerrainData terrainData, float defaultHeight, int defaultMaterialLayer, int heightOffsetX, int heightOffsetY) {

			// TODO

			// Get start and end in height data space
//			int heightStartX = (int)heightDataRect.xMin;
//			int heightStartY = (int)heightDataRect.yMin;
//			int heightEndX = (int)heightDataRect.xMax;
//			int heightEndY = (int)heightDataRect.yMax;

			// Height data
			{
				int size = terrainData.heightmapResolution;
				int startX = Mathf.Max(-heightOffsetX,0);
				int startY = Mathf.Max(-heightOffsetY,0);
				int endX = Mathf.Min(size - heightOffsetX,size);
				int endY = Mathf.Min(size - heightOffsetY,size);

				float[,] sourceHeights = terrainData.GetHeights(0, 0, size, size);
				float[,] targetHeights = new float[size, size];

				// Set default target value
				for (int y = 0; y < size; y++)
				{
					for (int x = 0; x < size; x++)
					{
						targetHeights[x, y] = defaultHeight;
					}
				}


				// Update heights
				for (int y = startY; y < endY; y++)
				{
					for (int x = startX; x < endX; x++)
					{
						int tx = x + heightOffsetX;
						int ty = y + heightOffsetY;
						targetHeights[tx, ty] = sourceHeights[x, y];
					}
				}

				terrainData.SetHeights(0, 0, targetHeights);
			}

			// Material 
			{
				float[,,] oldMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution);

				float[,,] newMaps = CreateAlphaMaps(terrainData.alphamapResolution, terrainData.alphamapLayers, defaultMaterialLayer);

				int size = terrainData.alphamapResolution;
				float scaleFactor = (float)size/(float)(terrainData.heightmapResolution - 1);
				int offsetX = (int)((float)heightOffsetX*scaleFactor); 
				int offsetY = (int)((float)heightOffsetY*scaleFactor); 

				int startX = Mathf.Max(-offsetX,0);
				int startY = Mathf.Max(-offsetY,0);
				int endX = Mathf.Min(size - offsetX,size);
				int endY = Mathf.Min(size - offsetY,size);

				for (int y = startY; y < endY; y++)
				{
					for (int x = startX; x < endX; x++)
					{
						int tx = x + offsetX;
						int ty = y + offsetY;

						for(int layer=0;layer<terrainData.alphamapLayers;layer ++) {
							newMaps[tx, ty, layer] = oldMaps[x, y, layer];
						}
					}
				}
				terrainData.SetAlphamaps(0, 0, newMaps);
			}


			// Detail
			{
				int layerCount = terrainData.detailPrototypes.Length;

				int size = terrainData.detailResolution;
				float scaleFactor = (float)size/(float)(terrainData.heightmapResolution - 1);
				int offsetX = (int)((float)heightOffsetX*scaleFactor); 
				int offsetY = (int)((float)heightOffsetY*scaleFactor); 

				int startX = Mathf.Max(-offsetX,0);
				int startY = Mathf.Max(-offsetY,0);
				int endX = Mathf.Min(size - offsetX,size);
				int endY = Mathf.Min(size - offsetY,size);

				for (int layer=0;layer< layerCount; layer ++)
				{
					int[,] oldMap = terrainData.GetDetailLayer(0, 0, terrainData.detailResolution, terrainData.detailResolution, layer);
					int[,] newMap = new int[terrainData.detailResolution, terrainData.detailResolution];

					for (int y = startY; y < endY; y++)
					{
						for (int x = startX; x < endX; x++)
						{
							newMap[x + offsetX, y + offsetY] = oldMap[x, y];
						}
					}
					terrainData.SetDetailLayer(0, 0, layer, newMap);
				}
			}

			// Trees
			{
				int heightMapSize = terrainData.heightmapResolution;
				float offsetZ = (float)heightOffsetX/(float)(heightMapSize);
				float offsetX = (float)heightOffsetY/(float)(heightMapSize);

				TreeInstance[] instances = terrainData.treeInstances;
				for (int i=0;i< instances.Length;i++)
				{
					Vector3 pos = instances[i].position;
					pos.x = pos.x + offsetX;
					pos.z = pos.z + offsetZ;
					instances[i].position = pos;
				}
				terrainData.treeInstances = instances;
			}
			terrainData.RefreshPrototypes();
		}
			
		/// <summary>
		/// Get height of terrain edge.
		/// </summary>
		/// <param name="terrain">Terrain to perform operation on</param>
		/// <param name="height">Calculated height</param>
		/// <returns> Return true if a single value can be found for the complete edge.</returns>
		public static bool GetEdgeHeight(Terrain terrain, out float bestHeight)
		{
			TerrainData terrainData = terrain.terrainData;

			Dictionary<float,int> histogram = new Dictionary<float,int>();

			GetEdgeHeightHistogram(terrainData, ref histogram);

			// Set best hight to first height we found
			var first = histogram.First();
			bestHeight = first.Key;

			// If histogram only have one value we can return
			if(histogram.Count == 1) {
				return true;
			}

			// If histogram has multiple height we need to return the one with the highest count
			int maxCount = 0;
			foreach(var pair in histogram) {
				if(pair.Value > maxCount) {
					bestHeight = pair.Key;
					maxCount = pair.Value;
				}
			}
			return false;
		}

		public static void DoubleTerrainPlaneSize(Terrain terrain, float defaultHeight, int defaultMaterialLayer)
		{
			TerrainData terrainData = terrain.terrainData;
			Vector3 oldSize = terrainData.size;
			//		Vector3 oldPivotLocalSpace = Vector3.Scale(terrainSource.PivotFraction, terrainSource.WorldSize);

			// Height data
			{
				int oldResolution = terrainData.heightmapResolution;

				int newResolution = (oldResolution - 1) * 2 + 1;

				// Create new heightmap with pivot height (so everything outside old data has default height)
				float[,] newHeightData = new float[newResolution, newResolution];
				for (int y = 0; y < newResolution; y++)
				{
					for (int x = 0; x < newResolution; x++)
					{
						newHeightData[x, y] = defaultHeight;
					}
				}

				// Copy old heightmap into new
				float[,] heightData = terrainData.GetHeights(0, 0, oldResolution, oldResolution);
				int halfOldResoltution = (oldResolution - 1) / 2;
				for (int y = 0; y < oldResolution; y++)
				{
					for (int x = 0; x < oldResolution; x++)
					{
						newHeightData[x + halfOldResoltution, y + halfOldResoltution] = heightData[x, y];
					}
				}

				// Set data
				terrainData.heightmapResolution = newResolution;
				terrainData.SetHeights(0, 0, newHeightData);
			}

			// Material
			{
				int layerCount = terrainData.alphamapLayers;
				int oldResolution = terrainData.alphamapResolution;
				int newResolution = oldResolution * 2;

				float[,,] oldMaps = terrainData.GetAlphamaps(0, 0, oldResolution, oldResolution);
				float[,,] newMaps = CreateAlphaMaps(newResolution, layerCount, defaultMaterialLayer);

				// Copy old data onto new
				int dataOffset = (oldResolution) / 2;
				for (int layer=0;layer< layerCount;layer ++)
				{
					for (int y = 0; y < oldResolution; y++)
					{
						for (int x = 0; x < oldResolution; x++)
						{
							newMaps[x + dataOffset, y + dataOffset, layer] = oldMaps[x, y, layer];
						}
					}
				}

				terrainData.alphamapResolution = newResolution;
				terrainData.SetAlphamaps(0, 0, newMaps);
			}

			// Detail
			{
				int layerCount = terrainData.detailPrototypes.Length;
				int oldResolution = terrainData.detailResolution;
				int newResolution = oldResolution * 2;
				int dataOffset = (oldResolution) / 2;

				for (int layer=0;layer< layerCount; layer ++)
				{
					int[,] oldMap = terrainData.GetDetailLayer(0, 0, oldResolution, oldResolution, layer);
					int[,] newMap = new int[newResolution, newResolution];

					for (int y = 0; y < oldResolution; y++)
					{
						for (int x = 0; x < oldResolution; x++)
						{
							newMap[x + dataOffset, y + dataOffset] = oldMap[x, y];
						}
					}

					terrainData.SetDetailResolution(newResolution, 16);
					terrainData.SetDetailLayer(0, 0, layer, newMap);
				}
			}

			// Trees
			{
				float scale = 0.5f;
				float offset = 0.25f;

				TreeInstance[] instances = terrainData.treeInstances;
				for (int i=0;i< instances.Length;i++)
				{
					Vector3 pos = instances[i].position;
					pos.x = pos.x * scale + offset;
					pos.z = pos.z * scale + offset;
					instances[i].position = pos;
				}
				terrainData.treeInstances = instances;
			}
			terrainData.RefreshPrototypes();

			// Set terrain size (we need to set it here as some data set functions seems to edit it)
			terrainData.size = Vector3.Scale(oldSize, new Vector3(2.0f, 1.0f, 2.0f));

			//		Vector3 oldPivot = terrainSource.PivotFraction;
			//		terrainSource.PivotFraction = Vector3.Scale(terrainSource.PivotFraction, new Vector3(0.5f, 1, 0.5f)) + new Vector3(0.25f, 0, 0.25f);
			//
			//		Vector3 newPivotLocalSpace = Vector3.Scale(terrainSource.PivotFraction, terrainSource.WorldSize);
			//		terrainSource.transform.position -= newPivotLocalSpace - oldPivotLocalSpace;
		}

		public static void HalveTerrainPlaneSize(Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;


			Vector3 oldSize = terrainData.size;
			//			Vector3 oldPivotLocalSpace = Vector3.Scale(terrainSource.PivotFraction, terrainSource.WorldSize);

			// Height data
			{
				int oldResolution = terrainData.heightmapResolution;
				int newResolution = (oldResolution - 1) / 2 + 1;

				float[,] newHeightData = new float[newResolution, newResolution];
				float[,] heightData = terrainData.GetHeights(0, 0, oldResolution, oldResolution);
				int halfNewResoltution = (newResolution - 1) / 2;
				for (int y = 0; y < newResolution; y++)
				{
					for (int x = 0; x < newResolution; x++)
					{
						newHeightData[x, y] = heightData[x + halfNewResoltution, y + halfNewResoltution];
					}
				}
				terrainData.heightmapResolution = newResolution;
				terrainData.SetHeights(0, 0, newHeightData);
			}

			// Material
			{
				int layerCount = terrainData.alphamapLayers;
				int oldResolution = terrainData.alphamapResolution;
				int newResolution = oldResolution / 2;

				float[,,] oldMaps = terrainData.GetAlphamaps(0, 0, oldResolution, oldResolution);
				float[,,] newMaps = new float[newResolution, newResolution, layerCount];

				// Copy old data onto new
				int dataOffset = (newResolution) / 2;
				for (int layer = 0; layer < layerCount; layer++)
				{
					for (int y = 0; y < newResolution; y++)
					{
						for (int x = 0; x < newResolution; x++)
						{
							newMaps[x, y, layer] = oldMaps[x + dataOffset, y + dataOffset, layer];
						}
					}
				}

				terrainData.alphamapResolution = newResolution;
				terrainData.SetAlphamaps(0, 0, newMaps);
			}

			// Detail
			{
				int layerCount = terrainData.detailPrototypes.Length;
				int oldResolution = terrainData.detailResolution;
				int newResolution = oldResolution / 2;
				int dataOffset = (newResolution) / 2;

				for (int layer = 0; layer < layerCount; layer++)
				{
					int[,] oldMap = terrainData.GetDetailLayer(0, 0, oldResolution, oldResolution, layer);
					int[,] newMap = new int[newResolution, newResolution];

					for (int y = 0; y < newResolution; y++)
					{
						for (int x = 0; x < newResolution; x++)
						{
							newMap[x, y] = oldMap[x + dataOffset, y + dataOffset];
						}
					}

					terrainData.SetDetailResolution(newResolution, 16);
					terrainData.SetDetailLayer(0, 0, layer, newMap);
				}
			}

			// Trees
			{
				float scale = 2.0f;
				float offset = 0.25f;

				List<TreeInstance> instances = new List<TreeInstance>(terrainData.treeInstances);
				for (int i = 0; i < instances.Count; i++)
				{
					TreeInstance instance = instances[i];
					Vector3 pos = instance.position;
					pos.x = (pos.x - offset) * scale;
					pos.z = (pos.z - offset) * scale;
					instance.position = pos;
					instances[i] = instance;
				}
				instances.RemoveAll(item => item.position.x < 0 || item.position.x > 1 || item.position.y < 0 || item.position.y > 1);

				terrainData.treeInstances = instances.ToArray();
			}

			terrainData.RefreshPrototypes();

			// Set terrain size (we need to set it here as some data set functions seems to edit it)
			terrainData.size = Vector3.Scale(oldSize, new Vector3(0.5f, 1.0f, 0.5f));

			//			terrainSource.PivotFraction = Vector3.Scale(terrainSource.PivotFraction - new Vector3(0.25f, 0, 0.25f), new Vector3(2,1,2));

			//			Vector3 newPivotLocalSpace = Vector3.Scale(terrainSource.PivotFraction, terrainSource.WorldSize);
			//			terrainSource.transform.position -= newPivotLocalSpace - oldPivotLocalSpace;
		}
	}
}

#endif