using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PocketHammer
{
    public class TerrainCombinerInstance : MonoBehaviour
    {
        public TerrainCombinerSource source = null;
        public Vector2 position = Vector2.zero;
        public float rotation = 0;
        public Vector2 size = Vector2.one;

        public Vector3 WorldSize
        {
            get { return source == null ? Vector3.zero : Vector3.Scale(source.WorldSize, transform.localScale); }
        }
        
        public Vector3 SourceWorldSize
        {
            get { return source == null ? Vector3.zero : source.WorldSize; }
        }
        

        public float WorldGroundHeight
        {
            get { return source == null ? 0 : source.GroundLevelFraction * WorldSize.y; }
        }

        public Terrain SouceTerrain
        {
            get { return source == null ? null : source.Terrain; }
        }
    }
}