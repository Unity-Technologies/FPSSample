using UnityEngine;
using UnityEditor;
using System;

public class GeometryImporter : AssetPostprocessor
{
    uint m_Version = 2;
    public override uint GetVersion() {return m_Version;}

	void OnPreprocessModel()
	{
		var importer = assetImporter as ModelImporter;
		if (importer != null)
		{
			importer.addCollider = false;      
			importer.importMaterials = false;
			//importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
			//importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
		}
	}

	void OnPostprocessModel(GameObject go)
	{
		AddColisionRecursively(go);
	}

	void AddColisionRecursively(GameObject go)
	{
		if (go.name.StartsWith("collision_", StringComparison.InvariantCultureIgnoreCase))
		{
			go.AddComponent<MeshCollider>();
			Editor.DestroyImmediate(go.GetComponent<MeshRenderer>());
			
			if(go.name.StartsWith("collision_detail", StringComparison.InvariantCultureIgnoreCase))
			{
				go.layer = LayerMask.NameToLayer("collision_detail");
			}
			else
			{
				if(go.name.StartsWith("collision_player", StringComparison.InvariantCultureIgnoreCase))
				{
					go.layer = LayerMask.NameToLayer("collision_player");
				}
			}
			
		}
		foreach (Transform g in go.transform)
		{
			AddColisionRecursively(g.gameObject);
		}
	}
}