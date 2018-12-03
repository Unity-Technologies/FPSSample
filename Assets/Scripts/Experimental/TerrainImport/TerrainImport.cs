using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TerrainImport : MonoBehaviour
{
    [Serializable]
    public class GeneralSetup
    {
        public Vector3 size = new Vector3(1000,300,1000);
    }

    [Serializable]
    public class HeightmapSetup
    {
        public Texture2D heightMap;
    }
    
    [Serializable]
    public class MaterialLayer
    {
        public TerrainLayer material;
        public Texture2D data;
        public int dataChannel;
    }

    [Serializable]
    public class MaterialSetup
    {
        public TerrainLayer baseMaterial;
        public List<MaterialLayer> layers = new List<MaterialLayer>();
    }  

    [Serializable]
    public class PropLayer
    {
        public Texture2D map;
        public TerrainPropSet propSet;
    }  

    [Serializable]
    public class PropSetup
    {
        public List<PropLayer> layers = new List<PropLayer>();
    }  
    
    public GeneralSetup generalSetup = new GeneralSetup();
    public HeightmapSetup heightSetup = new HeightmapSetup();
    public MaterialSetup materialSetup = new MaterialSetup();
    public PropSetup propSetup = new PropSetup();
}
