using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LightProbeGeneratorAssetPostprocessor : AssetPostprocessor
{

    uint m_Version = 2;
    public override uint GetVersion() {return m_Version;}

    void OnPostprocessModel(GameObject go)
    {
        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null)
            return;

        for (int i = 0;i<go.transform.childCount;i++)
        {
            Transform child = go.transform.GetChild(i);

            MeshFilter meshFilter = child.gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                continue;

            if (child.name.Contains("LPG_"))
            {
                List<Vector3> probePositions = new List<Vector3>();

                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                foreach (var vert in vertices)
                    probePositions.Add(vert);

                foreach (var component in child.gameObject.GetComponents<Component>())
                {
                    if (component is Transform)
                        continue;
                    Editor.DestroyImmediate(component);
                }
                    

                LightProbeGroup lightProbeGroup = child.gameObject.AddComponent<LightProbeGroup>();
                lightProbeGroup.probePositions = probePositions.ToArray();

                // For now we assume there is only one of these and end iteration
                return;
            }
        }

    }
}
