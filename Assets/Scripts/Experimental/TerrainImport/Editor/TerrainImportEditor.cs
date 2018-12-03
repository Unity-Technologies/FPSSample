
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;
using Random = UnityEngine.Random;


// Bridson’s algorithm
public class PoissonDiscSampling
{
    public enum GridDataState
    {
        free,
        set,
        illegal,
    }
    
    
    public struct PropGridData
    {
        public GridDataState state;
        public float2 worldPos;
        public float minDist;    // TODO (mogensh) get rid of this and always use density ?
        public float density;
    }

    public class PropData
    {
        public int gridIndex;
        public float2 pos;
    }

    public PropGridData[] propGrid;
    public List<PropData> activePropData;
    public List<PropData> propData;
    public float2 worldSize;
    public int propGridWidth;
    public int propGridHeight;
    public float gridSize;

    public void Setup(float2 worldSize, float minDistMin)
    {
        this.worldSize = worldSize;
        gridSize = minDistMin / Mathf.Sqrt(2);
        propGridWidth = Mathf.CeilToInt(worldSize.x / gridSize);
        propGridHeight = Mathf.CeilToInt(worldSize.y / gridSize);
        propGrid = new PropGridData[propGridWidth * propGridHeight];
        activePropData = new List<PropData>();
        propData = new List<PropData>();

        for (int i = 0; i < propGrid.Length; i++)
        {
            propGrid[i].minDist = minDistMin;
        }
    }
    
    
    public void Calculate()
    {
        int seedIndex = 0;

        while (seedIndex < propGrid.Length)
        {
            if (propGrid[seedIndex].state == GridDataState.free)
            {
                var gridX = seedIndex % propGridWidth;
                var gridY = (seedIndex-gridX)/propGridWidth;
                // TODO (mogensh) perhaps place all props in grid space to avoid all grid size calculations (until mapped to world space)
                var point = new float2((gridX + Random.value) * gridSize, (gridY + Random.value) * gridSize);

                if (!FindPointInRange(point, propGrid[seedIndex].minDist))
                {
                    RegisterPoint(point, seedIndex);
                    Debug.DrawLine(new Vector3(point.x,0,point.y),new Vector3(point.x,100,point.y),Color.magenta,10);
                }
            }
        
            while(activePropData.Count > 0)
            {
                var data = activePropData[Random.Range(0,activePropData.Count)];

                var gridData = propGrid[data.gridIndex];
                
                bool pointAccepted = false;
                for (int j = 0; j < 30; j++)
                {
                    var angle = Random.Range(0f, 360f);
                    var dir = new float2(Mathf.Cos(angle),Mathf.Sin(angle));
                    var dist = Random.Range(gridData.minDist, 2 * gridData.minDist);
                    var point = data.pos + dir * dist;

                    if (point.x < 0 || point.x >= worldSize.x || point.y < 0 || point.y >= worldSize.y)
                        continue;
                
                    var gridIndex = GetAvailableGridIndex(point);
                    if (gridIndex == -1)
                        continue;
                
                
                    if(FindPointInRange(point, gridData.minDist))
                        continue;
                
                    
                    
                    
                    RegisterPoint(point, gridIndex);
                    pointAccepted = true;
                    break;
                }

                if (!pointAccepted)
                {
                    activePropData.Remove(data);
                    propData.Add(data);
                }
            }

            seedIndex++;
        }
    }

    private const int k_debugMod = 1;
    
    public void MapDesity(ref float[,] data, float minDensity, float maxDensity, float distanceStart, float distanceEnd)
    {
        int i = 0;
        float worldX;
        float worldY =  gridSize*0.5f;
        int dataDimensionX = data.GetLength(0);
        int dataDimensionY = data.GetLength(1);
        float scaleX = dataDimensionX / worldSize.x;
        float scaleY = dataDimensionY / worldSize.y;

        var densityFactor = 1.0f / (maxDensity - minDensity);
        
        var minDistRange = distanceEnd - distanceStart;
        for (int y = 0; y < propGridHeight; y++)
        {
            worldX = gridSize*0.5f;
            for (int x = 0; x < propGridWidth; x++)
            {
                var maskX = Mathf.FloorToInt(worldY *scaleX) - 1;
                var maskY = Mathf.FloorToInt(worldX *scaleY) - 1;
                
                if(maskX < 0 || maskY < 0 || maskX >= dataDimensionX || maskY >= dataDimensionY)
                {
                    propGrid[i].state = GridDataState.illegal;
                    
//                    if(x%k_debugMod == 0 && y%k_debugMod == 0)
//                        Debug.DrawLine(new Vector3(worldX,0,worldY),new Vector3(worldX,100,worldY),Color.magenta,10);
                }
                else
                {
                    var density = data[maskX, maskY];
                    if (density == 0 || density < minDensity || density > maxDensity)
                    {
                        propGrid[i].state = GridDataState.illegal;
//                        if(x%k_debugMod == 0 && y%k_debugMod == 0)
//                            Debug.DrawLine(new Vector3(worldX,0,worldY),new Vector3(worldX,100,worldY),Color.red,10);
                    }
                    else
                    {
                        propGrid[i].state = GridDataState.free;

                        propGrid[i].minDist = distanceStart + (density - minDensity)*densityFactor*minDistRange;
                        propGrid[i].density = density;
                        
//                        if(x%k_debugMod == 0 && y%k_debugMod == 0)
//                            Debug.DrawLine(new Vector3(worldX,0,worldY),new Vector3(worldX,propGrid[i].minDist,worldY),Color.green,10);
                    }
                }
                
                worldX += gridSize;
                i++;
            }

            worldY += gridSize;
        }
        
    }


    public bool FindPointInRange(float2 pos, float dist)
    {
        var minPos = math.clamp(pos - new float2(dist), float2.zero, worldSize);
        var maxPos = math.clamp(pos + new float2(dist), float2.zero, worldSize);

        var minGrid = (int2)math.floor(minPos / gridSize);
        var maxGrid = (int2)math.floor(maxPos / gridSize);

        for (var y = minGrid.y; y <= maxGrid.y; y++)
        {
            for (var x = minGrid.x; x <= maxGrid.x; x++)
            {
                var gridIndex = x + y * propGridWidth;
                var gridData = propGrid[gridIndex];

                if (gridData.state != GridDataState.set)
                    continue;

                if (math.distance(pos, gridData.worldPos) > dist)
                    continue;

                return true;
            }
        }
        return false;
    }
    
    public void RegisterPoint(float2 pos, int gridIndex)
    {
        activePropData.Add(new PropData
        {
            pos = pos,
            gridIndex = gridIndex,
        });

        propGrid[gridIndex].state = GridDataState.set;
        propGrid[gridIndex].worldPos = pos;
    }

    public int GetAvailableGridIndex(float2 pos)
    {
        var gridX = Mathf.FloorToInt(pos.x / gridSize);
        var gridY = Mathf.FloorToInt(pos.y / gridSize);
        var gridIndex = gridX + gridY * propGridWidth;
        
        var gridData = propGrid[gridIndex];
        if (gridData.state != GridDataState.free)
            return -1;

        return gridIndex;
    }
}


[CustomEditor(typeof(TerrainImport))]
public class TerrainImportEditor : Editor
{
    public static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    private static bool showGeneral = false;
    private static bool showHeight = false;
    private static bool showMaterialLayers = false;
    private static bool showPropLayers = false;

    PoissonDiscSampling sampler = new PoissonDiscSampling();
    
    public override void OnInspectorGUI()
    {
        var terrainImport = target as TerrainImport;

        var generalButtonText = showGeneral ? "General" : "General";
        if (GUILayout.Button(generalButtonText, EditorStyles.toolbarDropDown))
        {
            showGeneral = !showGeneral;
        }
        if (showGeneral)
        {
            var genSetup = serializedObject.FindProperty("generalSetup");
            EditorGUILayout.PropertyField(genSetup.FindPropertyRelative("size"));
        }

        var heightButtonText = showHeight ? "Height" : "Height";
        if (GUILayout.Button(heightButtonText, EditorStyles.toolbarDropDown))
        {
            showHeight = !showHeight;
        }
        if (showHeight)
        {
            var heightSetup = serializedObject.FindProperty("heightSetup");
            EditorGUILayout.PropertyField(heightSetup.FindPropertyRelative("heightMap"));
            
            if (GUILayout.Button("Update"))
            {
                stopwatch.Reset();
                stopwatch.Start();
                UpdateHeight();
            }
        }
        

        var materialLayerButtonText = showMaterialLayers ? "Material" : "Material";
        if (GUILayout.Button(materialLayerButtonText, EditorStyles.toolbarDropDown))
        {
            showMaterialLayers = !showMaterialLayers;
        }
        if (showMaterialLayers)
        {
            var setup = serializedObject.FindProperty("materialSetup");
            
            EditorGUILayout.ObjectField(setup.FindPropertyRelative("baseMaterial"));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Layers");

            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                terrainImport.materialSetup.layers.Add(new TerrainImport.MaterialLayer());
                Repaint();
            }
            
            GUILayout.EndHorizontal();
            
            
            var layers = setup.FindPropertyRelative("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);

                var mat = element.FindPropertyRelative("material");
            
                GUILayout.BeginHorizontal();
            
                GUILayout.Space(10);
                
                var layerName = mat.objectReferenceValue != null ? mat.objectReferenceValue.name : "<undefined>"; 
                GUILayout.Label(layerName);
                if (GUILayout.Button("^", EditorStyles.boldLabel, GUILayout.Width(30)))
                {
                    if (i > 0)
                    {
                        var temp = terrainImport.materialSetup.layers[i];
                        terrainImport.materialSetup.layers[i] =
                            terrainImport.materialSetup.layers[i - 1];
                        terrainImport.materialSetup.layers[i - 1] = temp;
                    }
                }
                if (GUILayout.Button("v", GUILayout.Width(30)))
                {
                    if (i < terrainImport.materialSetup.layers.Count - 1)
                    {
                        var temp = terrainImport.materialSetup.layers[i];
                        terrainImport.materialSetup.layers[i] =
                            terrainImport.materialSetup.layers[i + 1];
                        terrainImport.materialSetup.layers[i + 1] = temp;
                    }
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    terrainImport.propSetup.layers.RemoveAt(i);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                
                GUILayout.Space(20);
                
                GUILayout.BeginVertical();

                var end = element.GetEndProperty();
                element.NextVisible(true);
                do
                {
                    EditorGUILayout.PropertyField(element);
                } while (element.NextVisible(false) && !SerializedProperty.EqualContents(element, end));
                
                GUILayout.EndVertical();
                
                GUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Update"))
            {
                stopwatch.Reset();
                stopwatch.Start();
                UpdateMaterials();
            }
        }


        var propButtonText = showHeight ? "Props" : "Props";
        if (GUILayout.Button(propButtonText, EditorStyles.toolbarDropDown))
        {
            showPropLayers = !showPropLayers;
        }
        if (showPropLayers)
        {
            var setup = serializedObject.FindProperty("propSetup");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layers");

            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                terrainImport.propSetup.layers.Add(new TerrainImport.PropLayer());
                Repaint();
            }
            
            GUILayout.EndHorizontal();

            
            var layers = setup.FindPropertyRelative("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);

                var propSet = element.FindPropertyRelative("propSet");
            
                GUILayout.BeginHorizontal();
            
                GUILayout.Space(10);
                
                var layerName = propSet.objectReferenceValue != null ? propSet.objectReferenceValue.name : "<undefined>"; 
                GUILayout.Label(layerName);
                if (GUILayout.Button("^", EditorStyles.boldLabel, GUILayout.Width(30)))
                {
                    if (i > 0)
                    {
                        var temp = terrainImport.propSetup.layers[i];
                        terrainImport.propSetup.layers[i] =
                            terrainImport.propSetup.layers[i - 1];
                        terrainImport.propSetup.layers[i - 1] = temp;
                    }
                }
                if (GUILayout.Button("v", GUILayout.Width(30)))
                {
                    if (i < terrainImport.materialSetup.layers.Count - 1)
                    {
                        var temp = terrainImport.propSetup.layers[i];
                        terrainImport.propSetup.layers[i] =
                            terrainImport.propSetup.layers[i + 1];
                        terrainImport.propSetup.layers[i + 1] = temp;
                    }
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    terrainImport.propSetup.layers.RemoveAt(i);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                
                GUILayout.Space(20);
                
                GUILayout.BeginVertical();

                var end = element.GetEndProperty();
                element.NextVisible(true);
                do
                {
                    EditorGUILayout.PropertyField(element);
                } while (element.NextVisible(false) && !SerializedProperty.EqualContents(element, end));

                GUILayout.EndVertical();
                
                GUILayout.EndHorizontal();
            }
            
            
            if (GUILayout.Button("Update"))
            {
                stopwatch.Reset();
                stopwatch.Start();
                UpdateProps();
            }
        }
        
        serializedObject.ApplyModifiedProperties();
        
        GUILayout.Space(100);
        
        if (GUILayout.Button("Update ALL"))
        {
            stopwatch.Reset();
            stopwatch.Start();
            UpdateHeight();
            UpdateMaterials();
            UpdateProps();
        }
    }


    void UpdateHeight()
    {
        var terrainImport = target as TerrainImport;
        var time = stopwatch.Elapsed;
        UpdateHeight(terrainImport);
        GameDebug.Log("Height map applied [" + (stopwatch.Elapsed - time).Milliseconds +"ms]");  
            
    }

    void UpdateMaterials()
    {
        var terrainImport = target as TerrainImport;
        var time = stopwatch.Elapsed;
        UpdateAlphaMaps(terrainImport);
        GameDebug.Log("Material layers applied [" + (stopwatch.Elapsed - time).Milliseconds +"ms]"); 
    }
    
    
    void UpdateProps()
    {
        var terrainImport = target as TerrainImport;
        var time = stopwatch.Elapsed;

        var terrain = terrainImport.GetComponent<Terrain>();
        var terrainData = terrain.terrainData;
        terrainData.treeInstances = new TreeInstance[0];
        
        for (int i = 0; i < terrainImport.propSetup.layers.Count; i++)
        {
            UpdatePropLayer(terrainImport, terrainImport.propSetup.layers[i]);
        }
        
        GameDebug.Log("Prop layers applied [" + (stopwatch.Elapsed - time).Milliseconds +"ms]"); 
    }
    
    void UpdatePropLayer(TerrainImport terrainImport, TerrainImport.PropLayer layer)
    {
        var time = stopwatch.Elapsed;

        var terrain = terrainImport.GetComponent<Terrain>();
        var terrainData = terrain.terrainData;


        if (layer.propSet == null)
            return;

        var instanceList = new List<TreeInstance>(terrainData.treeInstances);


        foreach (var propData in layer.propSet.props)
        {
            sampler.Setup(new float2(terrainData.size.x, terrainData.size.z), Mathf.Min(propData.distanceStart,propData.distanceEnd));

            if (layer.map != null)
            {
                float[,] mapData = new float[layer.map.width, layer.map.height];
                ReadChannelFromTexture(layer.map, 0, ref mapData);
                sampler.MapDesity(ref mapData, propData.minDensity, propData.maxDensity, propData.distanceStart, propData.distanceEnd);
            }

            sampler.Calculate();

            var protoIndex = FindTreeProtoIndex(terrainData, propData.prefab);
            if (protoIndex == -1)
            {
                var protoList = new List<TreePrototype>(terrainData.treePrototypes);
                protoIndex = protoList.Count;
                protoList.Add(new TreePrototype
                {
                    prefab = propData.prefab
                });
                terrainData.treePrototypes = protoList.ToArray();
                EditorUtility.SetDirty(terrainData);
                EditorUtility.SetDirty(terrain);
            }
        

            for (int i = 0; i < sampler.propData.Count; i++)
            {
                var prop = sampler.propData[i];
                var gridData = sampler.propGrid[prop.gridIndex];
                
                var pos3D = new Vector3(prop.pos.x, 0, prop.pos.y);
                var worldPos = terrainImport.transform.TransformPoint(pos3D); 
            
                var height = terrain.SampleHeight(worldPos);

                var pos = new float3(sampler.propData[i].pos.x/terrainData.size.x, height/terrainData.size.y, 
                    sampler.propData[i].pos.y/terrainData.size.z);

                var densityFactor = 1.0f / (propData.maxDensity - propData.minDensity);
                var scaleRange = propData.scaleEnd - propData.scaleStart;
                var scale = propData.scaleStart + (gridData.density - propData.minDensity)*densityFactor*scaleRange;
                
                scale = scale + Random.Range(0, scale*propData.scaleRandomFraction);
                
                instanceList.Add(new TreeInstance
                {
                    prototypeIndex = protoIndex,
                    widthScale = scale,
                    heightScale = scale,
                    rotation = Random.Range(0,360),
                    color = propData.color,
                    lightmapColor = propData.lightmapColor,
                    position = pos
                });
            }            
        }
        
        
        


        terrainData.treeInstances = instanceList.ToArray();
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);
        
        GameDebug.Log("Prop layers applied [" + (stopwatch.Elapsed - time).Milliseconds +"ms]"); 
    }

    int FindTreeProtoIndex(TerrainData terrainData, GameObject prefab)
    {
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        for (int i = 0; i < terrainData.treePrototypes.Length; i++)
        {
            var protoPath = AssetDatabase.GetAssetPath(terrainData.treePrototypes[i].prefab);
            if (protoPath == prefabPath)
                return i;
        }

        return -1;
    }
    

    void UpdateAlphaMaps(TerrainImport terrainImport)
    {
        var terrain = terrainImport.GetComponent<Terrain>();
        var terrainData = terrain.terrainData;

        if (terrainImport.materialSetup.baseMaterial == null)
        {
            Debug.LogError("No baselayer defined!");
            return;
        }
        
        var width = 0;
        var height = 0;
        var sizeSet = false;
        var layerCount = 1;

        // Get size of data
        for (int i = 0; i < terrainImport.materialSetup.layers.Count; i++)
        {
            var layerData = terrainImport.materialSetup.layers[i].data;
            if (layerData != null)
            {
                width = layerData.width;
                height = layerData.height;
                break;
            }
        }
            
        
        
        for (int i = terrainImport.materialSetup.layers.Count - 1; i >= 0; i--)
        {
            var layer = terrainImport.materialSetup.layers[i];

            if(IsLayerValid(layer, width, height, true))
                layerCount++;
        }

        Debug.Log("Updating " + layerCount + " layers. Size:" + width + "," + height);
        
        var alphaMaps = new float[width,height,layerCount];
        float[,] data = new float[width, height];
        var terrainLayers = new List<TerrainLayer>();

        // Add base layer
        terrainLayers.Add(terrainImport.materialSetup.baseMaterial);
        Clear(alphaMaps, 0);
        
        
        for (int i = terrainImport.materialSetup.layers.Count -1; i >= 0; i--)
        {
            var layer = terrainImport.materialSetup.layers[i];
            if (!IsLayerValid(terrainImport.materialSetup.layers[i], width, height, false))
                continue;

            var layerIndex = terrainLayers.Count;
            terrainLayers.Add(layer.material);
            ReadChannelFromTexture(layer.data, layer.dataChannel, ref data);
            Blend(alphaMaps, layerIndex, data);
        }

        terrainData.terrainLayers = terrainLayers.ToArray();
        terrainData.alphamapResolution = width;
        terrainData.SetAlphamaps(0,0,alphaMaps);  
        
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);
    }

    bool IsLayerValid(TerrainImport.MaterialLayer materialLayer, int width, int height, bool logError)
    {
        if (materialLayer.material == null)
            return false;
        if (materialLayer.data == null)
            return false;
        
        var layerName = materialLayer.material != null ? materialLayer.material.name : "?";
        if (materialLayer.data.width != width || materialLayer.data.height != height)
        {
            if(logError)
                Debug.LogError("Layer:" + layerName + " data has invalid size");
            return false;
        }
        if (materialLayer.material.diffuseTexture.width != width || materialLayer.material.diffuseTexture.height != height)
        {
            if(logError)
                Debug.LogError("Layer:" + layerName + " layer diffuse texture has invalid size.");
            return false;
        }

        return true;
    }
    
    void Clear(float[,,] alphaMap, int layerIndex)
    {
        var width = alphaMap.GetLength(0);
        var height = alphaMap.GetLength(1);
        var layerCount = alphaMap.GetLength(2);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < layerCount; i++)
                {
                    if (i == layerIndex)
                        alphaMap[x, y, i] = 1;
                    else
                    {
                        alphaMap[x, y, i] = 0;
                    }
                }
            }
        }
    }

    void Blend(float[,,] alphaMap, int layerIndex, float[,] layerMask)
    {
        var width = alphaMap.GetLength(0);
        var height = alphaMap.GetLength(1);
        var layerCount = alphaMap.GetLength(2);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var src = layerMask[x, y];

                for (int i = 0; i < layerCount; i++)
                {
                    if (i == layerIndex)
                        alphaMap[x, y, i] = src;
                    else
                    {
                        alphaMap[x, y, i] *= 1 - src;
                    }
                }
            }
        }
    }
    
    void UpdateHeight(TerrainImport terrainImport)
    {
        
        // Height
        if (terrainImport.heightSetup.heightMap == null)
            return;
        var format = terrainImport.heightSetup.heightMap.format;


        GameDebug.Log("Update height");

        // TODO: use Texture2D.GetRawTextureData ??

        
        var terrain = terrainImport.GetComponent<Terrain>();
        var terrainData = terrain.terrainData;
            
        terrainData.heightmapResolution = terrainImport.heightSetup.heightMap.width;
        terrainData.size = terrainImport.generalSetup.size; 

        var time = stopwatch.Elapsed;
        var data = new float[terrainImport.heightSetup.heightMap.width, terrainImport.heightSetup.heightMap.height];
        GameDebug.Log("Allocated buffer [" + (stopwatch.Elapsed - time).Milliseconds +"ms]");
        
        ReadChannelFromTexture(terrainImport.heightSetup.heightMap, 0, ref data);
        
        time = stopwatch.Elapsed;
        terrainData.SetHeights(0,0,data);
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);
        GameDebug.Log("Set heights [" + (stopwatch.Elapsed - time).Milliseconds +"ms]");      
    }
    
    void ReadChannelFromTexture(Texture2D texture, int channel, ref float[,] data) 
    {
        int width = texture.width;
        int height = texture.height;


        var time = stopwatch.Elapsed;
        var rawData = texture.GetRawTextureData();
        GameDebug.Log("Read raw data [" + (stopwatch.Elapsed - time).Milliseconds +"ms]");        
        
        time = stopwatch.Elapsed;
        Color[] colors = texture.GetPixels();
        GameDebug.Log("Read color data [" + (stopwatch.Elapsed - time).Milliseconds +"ms]");        

        time = stopwatch.Elapsed;
        int i=0;
        for(int x=0;x<width;x++) {
            for(int y=0;y<height;y++) {
                var color = colors[i++];
                data[x,y] = color[channel];
            }
        }
        GameDebug.Log("Apply data to buffer [" + (stopwatch.Elapsed - time).Milliseconds +"ms]");        
    }
}
