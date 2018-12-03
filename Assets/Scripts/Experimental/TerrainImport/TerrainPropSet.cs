using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "PropSet", menuName = "FPS Sample/Terrain/PropSet")]
public class TerrainPropSet : ScriptableObject
{
    [Serializable]
    public class Prop
    {
        public GameObject prefab;

        [Tooltip("Min desity before prop appears")]
        public float minDensity = 0;
        public float maxDensity = 1;

        //[CurveCustomDrawer(0,0,1,1,"#FF0000FF")]
        //public AnimationCurve curve;
        
        [Tooltip("Distance at lowest density")]
        public float distanceStart = 20;
        [Tooltip("Distance at highest density")]
        public float distanceEnd = 60;
        
        public float scaleStart = 1;
        public float scaleEnd = 1;
        public float scaleRandomFraction = 0;
        
        public Color32 color = Color.white;
        public Color32 lightmapColor = Color.white;
    }

    public List<Prop> props = new List<Prop>();
}
