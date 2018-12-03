#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// PRIORITY A
// TODO: Trees!
// TODO: Detail!
// BUG: instance scall does not work

// PRIORITY B
// world scale scaling and positioning 
// Move layers up/down

// PRIORITY C
// Rotation around center
// TODO: child objects
// BUG: error bleeding from top of terrain to button when combining (very high values .. e.g. debug x/y axis)
// TODO: default combiner terrain material

// PRIORITY D
// TODO; reuse/scale textures instead of re-instantiating
// TODO; use argb32 for height and convert float instead of argbf
// TODO: only sample dirty area (check how much instance have moved)
// TODO: find out way to listen for render done (so we dont need task system)
// TODO: use TerrainData.alphamapTextures instead of cache ?

// https://forum.unity3d.com/threads/where-to-install-your-assets-for-scripting-packages-editor-extensions.292674/



namespace PocketHammer
{
	[RequireComponent (typeof (Terrain))]
	public class TerrainCombiner : MonoBehaviour {

		[System.Serializable]
		public class InstanceData {
			public string displayName;
			public bool openInInspector = false;
			public TerrainCombinerSource source = null;
			public Vector2 position = Vector2.zero;
			public float rotation = 0;
			public Vector2 size = Vector2.one;
			public float heightSize = 1.0f;
		}

        public TerrainCombinerInstance[] Instances
        {
            get {
                return this.GetComponentsInChildren<TerrainCombinerInstance>();
            }
        }

		public float groundLevelFraction = 0.0f;

		public Terrain Terrain
		{
			get { return GetComponent<Terrain>(); }
		}
		
		public Vector3 WorldSize
		{
			get { return Terrain.terrainData.size; }
		}
		
		public static Vector2 CalcChildTerrainPlaneScale(Vector3 parentWorldSize, Vector3 childWorldSize, Vector2 scale) {

			Vector2 outScale;
			outScale.x = (childWorldSize.z*scale.x)/parentWorldSize.z;
			outScale.y = (childWorldSize.x*scale.y)/parentWorldSize.x;

			return outScale;
		}

		// Cache
		public bool CacheDirty = true;

		public class HeightmapCacheData {
			public Texture2D Texture;
			public float[,] ResultData;
			public RenderTexture RenderTarget;
		}
		public HeightmapCacheData HeightmapCache = new HeightmapCacheData();


		public class MaterialCacheData {
			public List<RenderTexture> RenderTextures = new List<RenderTexture>();
			public Texture2D Texture;
			public float[,,] ResultData;
		}
		public MaterialCacheData MaterialCache = new MaterialCacheData();
	}

}

#endif

