using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace PocketHammer
{
	[CustomEditor(typeof(TerrainCombinerSource))]
	public class TerrainCombinerSourceEditor : Editor {


		void OnEnable () {
			
		}

		public override void OnInspectorGUI()
		{
			var source = (TerrainCombinerSource)target;
			var terrain = source.GetComponent<Terrain>();

			source.GroundLevelFraction = EditorGUILayout.FloatField("HeightDataGround",source.GroundLevelFraction);

			if (GUILayout.Button("Calculate ground level")) {
				source.GroundLevelFraction = FindGroundLevel(terrain.terrainData);
				EditorUtility.SetDirty(source);
				EditorUtility.SetDirty(source.gameObject);
			}
			
			source.alphaMaterial = (TerrainLayer) EditorGUILayout.ObjectField("AlphaMaterial", source.alphaMaterial, typeof (TerrainLayer), false);

			if (GUILayout.Button("Add alpha to terrain")) {
				TCMaterialHelper.AddTerrainLayer(terrain.terrainData,source.alphaMaterial);
				EditorUtility.SetDirty(terrain.terrainData);
				EditorUtility.SetDirty(source);
				EditorUtility.SetDirty(source.gameObject);
			}

			source.CacheDirty = true;
		}


		float FindGroundLevel(TerrainData terrainData) {

			Dictionary<float,int> edgeHeightHistogram = new Dictionary<float, int>();

			// Add edge values to histogram
			int size = terrainData.heightmapResolution;
			float[,] heightData = terrainData.GetHeights(0,0,size,size);
			for(int i=0;i<size;i++) {
				AddHeight(edgeHeightHistogram,heightData[i,0]);
				AddHeight(edgeHeightHistogram,heightData[i,size-1]);
				AddHeight(edgeHeightHistogram,heightData[0,i]);
				AddHeight(edgeHeightHistogram,heightData[size-1,i]);
			}

			// Find height with highest count
			int maxCount = 0;
			float bestHeight = 0;
			foreach(var pair in edgeHeightHistogram) {
				if(pair.Value > maxCount) {
					bestHeight = pair.Key;
					maxCount = pair.Value;
				}
			}

			return bestHeight;
		}

		void AddHeight(Dictionary<float,int> histogram, float height) {
			if(histogram.ContainsKey(height)) {
				histogram[height] ++;
			}
			else {
				histogram.Add(height,1);
			}
		}
	}
}
