
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;



namespace PocketHammer
{

	[RequireComponent (typeof (Terrain))]
	public class TerrainCombinerSource : MonoBehaviour {

		public float GroundLevelFraction = 0.0f;
		public TerrainLayer alphaMaterial;

		// Cache
		[NonSerialized] public Texture2D CachedHeightmapTexture;
		[NonSerialized] public List<Texture2D> CachedMaterials = new List<Texture2D>();
		[NonSerialized] public bool CacheDirty = true;

		public Terrain Terrain
		{
			get { return GetComponent<Terrain>(); }
		}

		public Vector3 WorldSize
		{
			get { return Terrain.terrainData.size; }
		}
	}

}
