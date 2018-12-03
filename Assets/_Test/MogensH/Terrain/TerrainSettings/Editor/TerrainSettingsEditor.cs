using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;



namespace PocketHammer
{
	[CustomEditor(typeof(TerrainSettings))]
	public class TerrainSettingsEditor : Editor
	{
		private const string PrefName_AdjustPlane = "TerrainResizeEditor_AdjustPlane";
		private const string PrefName_AdjustHeight = "TerrainResizeEditor_AdjustHeight";
		private const string PrefName_AdjustResolution = "TerrainResizeEditor_AdjustResolution";
		private const string PrefName_KeepFeatureWorldPos = "TerrainResize_KeepFeatureWorldPos";
		private const string PrefName_UpdateChildPosition = "TerrainResize_UpdateChildPositions";
		private const string PrefName_ShowHalfSize = "TerrainResize_ShowHalfSize";



		public override void OnInspectorGUI()
		{
			int smallButtonSize = 50;
			int largeButtonSize = smallButtonSize*2 + 4;

			TerrainSettings terrainResizer = (TerrainSettings)target;

			Terrain terrain = terrainResizer.GetComponent<Terrain>();
			if(terrain == null) {
				EditorGUILayout.HelpBox("TerrainResizer needs to be attached to gameobject with a Terrain component", MessageType.Error);
				return;
			}

			TerrainData terrainData = terrain.terrainData;
			if(terrainData == null) {
				EditorGUILayout.HelpBox("Terrain has no valid terrain data", MessageType.Error);
				return;
			}
				
			// Plane
			{
//				bool show = DrawEditorPrefFoldout("Plane (XZ-axis)",PrefName_AdjustPlane);
//				if (show)
				{
					EditorGUILayout.LabelField("Planar Size (XZ-axis):", EditorStyles.boldLabel);
//					EditorGUI.indentLevel++;

					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField("Size:", GUILayout.Width(100));
					GUILayout.FlexibleSpace();
					if (GUILayout.Button(new GUIContent("Double","Doubles terrain size while maintaining terrain feature proportions"), GUILayout.Width(smallButtonSize)))
					{
						Vector3 oldPlaneSize = terrainData.size;
						oldPlaneSize.y = 0.0f;

						EditorSceneManager.MarkAllScenesDirty();

						float defaultHeight = 0;
						TerrainSettingsHelper.GetEdgeHeight(terrain, out defaultHeight);

						// TODO: find border material
						TerrainSettingsHelper.DoubleTerrainPlaneSize(terrain, defaultHeight, 0);

						TerrainSettingsHelper.AdjustTransform(terrain, -oldPlaneSize*0.5f, EditorPrefs.GetBool(PrefName_KeepFeatureWorldPos), EditorPrefs.GetBool(PrefName_UpdateChildPosition));
					}

					if (GUILayout.Button(new GUIContent("Halve","Halves terrain size while maintaining proportions of terrain features at the center of the terrain. This is an destructive action."), GUILayout.Width(smallButtonSize)))
					{
						Vector3 oldPlaneSize = terrainData.size;
						oldPlaneSize.y = 0.0f;

						if(EditorUtility.DisplayDialog("Halve terrain size?",
							"Are you sure you want to halve terrain size? This is a destructive action and all data outside center half of the terrain will be lost. Please use halfsize indicator to verify before continuing.", "Do it!", "Cancel")) {

							EditorSceneManager.MarkAllScenesDirty();

							TerrainSettingsHelper.HalveTerrainPlaneSize(terrain);

							TerrainSettingsHelper.AdjustTransform(terrain, oldPlaneSize*0.25f, EditorPrefs.GetBool(PrefName_KeepFeatureWorldPos), EditorPrefs.GetBool(PrefName_UpdateChildPosition));

						}

					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField("Center features:", GUILayout.Width(100));
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Center", GUILayout.Width(largeButtonSize)))
					{

						if(EditorUtility.DisplayDialog("Center Features?",
							"Are you sure you want to center all terrain features. Only height data is used to determine extend of features so non-height data might end up outside terrain and be lost.", "Do it!", "Cancel")) {

							EditorSceneManager.MarkAllScenesDirty();

							float edgeHeight;
							TerrainSettingsHelper.GetEdgeHeight(terrain, out edgeHeight);

							Rect bounds = new Rect();
							TerrainSettingsHelper.GetHeightDataBounds(terrainData, edgeHeight, ref bounds);

							int heightOffsetX = (terrainData.heightmapResolution - 1)/2 - (int)Mathf.Floor(bounds.center.x);
							int heightOffsetY = (terrainData.heightmapResolution - 1)/2 - (int)Mathf.Floor(bounds.center.y);

							TerrainSettingsHelper.MoveData(terrainData, edgeHeight, 0, heightOffsetX, heightOffsetY);

							Vector2 deltaCenterDataSpace = new Vector2(terrainData.heightmapResolution*0.5f,terrainData.heightmapResolution*0.5f) - bounds.center;
							Vector3 deltaCenterWorldSpace = new Vector3(deltaCenterDataSpace.y*terrainData.heightmapScale.x, 0.0f, deltaCenterDataSpace.x*terrainData.heightmapScale.z);

							TerrainSettingsHelper.AdjustTransform(terrain, deltaCenterWorldSpace, EditorPrefs.GetBool(PrefName_KeepFeatureWorldPos), EditorPrefs.GetBool(PrefName_UpdateChildPosition));
						}
					}
					EditorGUILayout.EndHorizontal();


//					EditorGUI.indentLevel--;
				}
			}

			// Height
			{
//				bool show = DrawEditorPrefFoldout("Height (Y-axis):",PrefName_AdjustHeight);
//				if (show)
				{
					EditorGUILayout.LabelField("Height (Y-axis):", EditorStyles.boldLabel);

//					EditorGUI.indentLevel ++;

                    // TODO Auto adjust ceiling/floor

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Adjust ceiling", GUILayout.Width(100));
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Up", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						float delta = 10.0f;
						TerrainSettingsHelper.AdjustCeiling(terrainData, delta);
					}
					if (GUILayout.Button("Down", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						float delta = -10.0f;
						TerrainSettingsHelper.AdjustCeiling(terrainData, delta);
					}
					EditorGUILayout.EndHorizontal();


					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Adjust floor", GUILayout.Width(100));
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Up", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

//						Vector3 oldSize = terrainData.size;
						float delta = 10.0f;

							
						if(TerrainSettingsHelper.AdjustFloor(terrainData, delta)) {

							TerrainSettingsHelper.AdjustTransform(terrain, new Vector3(0,delta,0), EditorPrefs.GetBool(PrefName_KeepFeatureWorldPos), EditorPrefs.GetBool(PrefName_UpdateChildPosition));
						}


					}
					if (GUILayout.Button("Down", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();	

//						Vector3 oldSize = terrainData.size;
						float delta = -10f;
						if(TerrainSettingsHelper.AdjustFloor(terrainData, delta)) {

							TerrainSettingsHelper.AdjustTransform(terrain, new Vector3(0,delta,0), EditorPrefs.GetBool(PrefName_KeepFeatureWorldPos), EditorPrefs.GetBool(PrefName_UpdateChildPosition));
						}
					}
					EditorGUILayout.EndHorizontal();

//					EditorGUI.indentLevel --;
				}
			}

			// Resolution
			{
//				bool show = DrawEditorPrefFoldout("Resolution",PrefName_AdjustResolution);
//				if (show)
				{
					int minWidth = 20;
					int valueWidth = 60;

					EditorGUILayout.LabelField("Resolution:", EditorStyles.boldLabel);

//					EditorGUI.indentLevel ++;

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Heightmap", GUILayout.MinWidth(minWidth));
					GUILayout.FlexibleSpace();
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField(terrainData.heightmapResolution, GUILayout.Width(valueWidth));
					EditorGUI.EndDisabledGroup();
					if (GUILayout.Button("Double", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						Texture2D texture = null;
						TerrainSettingsHelper.HeightDataDouble(terrainData, ref texture);
//						TerrainResizerHelper.DoubleHeightResolution(terrainData);	
					}
					if (GUILayout.Button("Halve", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						// TODO: destructive warning

						Texture2D texture = null;
						TerrainSettingsHelper.HeightDataHalve(terrainData, ref texture);	
					}
					EditorGUILayout.EndHorizontal();


					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Control (Material)", GUILayout.MinWidth(minWidth));
					GUILayout.FlexibleSpace();
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField(terrainData.alphamapResolution, GUILayout.Width(valueWidth));
					EditorGUI.EndDisabledGroup();
					if (GUILayout.Button("Double", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						TerrainSettingsHelper.ChangeAlphaMapResolution(terrainData,terrainData.alphamapResolution*2);
					}
					if (GUILayout.Button("Halve", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						TerrainSettingsHelper.ChangeAlphaMapResolution(terrainData,terrainData.alphamapResolution/2);
					}
					EditorGUILayout.EndHorizontal();


					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Detail", GUILayout.MinWidth(minWidth));
					GUILayout.FlexibleSpace();
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField(terrainData.detailResolution, GUILayout.Width(valueWidth));
					EditorGUI.EndDisabledGroup();
					if (GUILayout.Button("Double", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						// TODO
					}
					if (GUILayout.Button("Halve", GUILayout.Width(smallButtonSize)))
					{
						EditorSceneManager.MarkAllScenesDirty();

						// TODO
					}
					EditorGUILayout.EndHorizontal();


//					EditorGUI.indentLevel --;
				}
			}

			// Resolution
			{
				{
					EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);

					DrawEditorPrefToggle(new GUIContent("Show half size","Show box indicating half size of terrain. Use this to visualize what data is lost when halving size"),PrefName_ShowHalfSize);
					DrawEditorPrefToggle(new GUIContent("Keep feature world location","Ensure all terrain features will keep world location by adjusting terrain origin when modifying terrain"),PrefName_KeepFeatureWorldPos);
					DrawEditorPrefToggle(new GUIContent("Children follow features","Ensure location of children are updated so they follow terrain features when terrain is modified"),PrefName_UpdateChildPosition);
				}
			}


            if(GUILayout.Button("X axis"))
            {
                var map = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);
                for(int i=0;i<50;i++)
                {
                    map[i, 0] = 1;
                }
                terrain.terrainData.SetHeights(0, 0, map);
            }
            if (GUILayout.Button("Y axis"))
            {
                var map = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);
                for (int i = 0; i < 50; i++)
                {
                    map[0, i] = 1;
                }
                terrain.terrainData.SetHeights(0, 0, map);
            }
        }



        void OnSceneGUI()
        {
            TerrainSettings terrainResizer = (TerrainSettings)target;
            Terrain terrain = terrainResizer.GetComponent<Terrain>();

            // Draw bounds
            Handles.color = Color.yellow;
            Vector3 size = Vector3.Scale(terrain.terrainData.size, terrainResizer.transform.localScale);
            Handles.DrawWireCube(terrainResizer.transform.position + size*0.5f, size);
        }



        [DrawGizmo(GizmoType.InSelectionHierarchy)]
		static void RenderGizmos(TerrainSettings resizer, GizmoType gizmoType)       // TODO remove this render gizmo stuff
		{
			Terrain terrain = resizer.GetComponent<Terrain>();
			TerrainData terrainData = terrain.terrainData;


			bool bAdjustPlane = EditorPrefs.GetBool(PrefName_AdjustPlane, false);
			bool bAdjustHeight = EditorPrefs.GetBool(PrefName_AdjustHeight, false);
			bool bShowHalfSize = EditorPrefs.GetBool(PrefName_ShowHalfSize, false);

			// Border
			if(bAdjustPlane || bAdjustHeight) {
				DrawBox(terrain, Vector3.zero,terrainData.size, Color.white);
			}

			// Half size
			if(bAdjustPlane && bShowHalfSize) {
				float minX = terrainData.size.x*0.25f;
				float minZ = terrainData.size.z*0.25f;
				float maxX = terrainData.size.x - minX;
				float maxZ = terrainData.size.z - minZ;
				DrawBox(terrain, new Vector3(minX,0,minZ),new Vector3(maxX,terrainData.size.y, maxZ), Color.yellow);
			}
		}


		private static void DrawBox(Terrain terrain, Vector3 min,Vector3 max, Color color) {
//			TerrainData terrainData = terrain.terrainData;

			Vector3 p0 = terrain.transform.TransformPoint(new Vector3(min.x, min.y, min.z));
			Vector3 p1 = terrain.transform.TransformPoint(new Vector3(max.x, min.y, min.z));
			Vector3 p2 = terrain.transform.TransformPoint(new Vector3(max.x, min.y, max.z));
			Vector3 p3 = terrain.transform.TransformPoint(new Vector3(min.x, min.y, max.z));
			Vector3 up = terrain.transform.up * max.y;

			Gizmos.color = color;

			Gizmos.DrawLine(p0, p1);
			Gizmos.DrawLine(p1, p2);
			Gizmos.DrawLine(p2, p3);
			Gizmos.DrawLine(p3, p0);

			Gizmos.DrawLine(p0 + up, p1 + up);
			Gizmos.DrawLine(p1 + up, p2 + up);
			Gizmos.DrawLine(p2 + up, p3 + up);
			Gizmos.DrawLine(p3 + up, p0 + up);

			Gizmos.DrawLine(p0, p0 + up);
			Gizmos.DrawLine(p1, p1 + up);
			Gizmos.DrawLine(p2, p2 + up);
			Gizmos.DrawLine(p3, p3 + up);
		}

		private bool DrawEditorPrefFoldout(string displayName, string prefName)
		{
			bool shown = EditorPrefs.GetBool(prefName);
			bool newVal = EditorGUILayout.Foldout(shown, displayName);


			if (newVal != shown)
			{
				EditorPrefs.SetBool(prefName,newVal);
				SceneView.RepaintAll();
			}
			return newVal;
		}


		private void DrawEditorPrefToggle(GUIContent content,string editorPref)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(content);
			GUILayout.FlexibleSpace();
			bool bKeepFeatureWorldPos = EditorPrefs.GetBool(editorPref,true);
			bool bNewValue = EditorGUILayout.Toggle(bKeepFeatureWorldPos, GUILayout.Width(30));
			if(bNewValue != bKeepFeatureWorldPos) {
				EditorPrefs.SetBool(editorPref,bNewValue);
				SceneView.RepaintAll();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawEditorPrefToggle(string label,string editorPref)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label);
			GUILayout.FlexibleSpace();
			bool bKeepFeatureWorldPos = EditorPrefs.GetBool(editorPref,true);
			bool bNewValue = EditorGUILayout.Toggle(bKeepFeatureWorldPos, GUILayout.Width(30));
			if(bNewValue != bKeepFeatureWorldPos) {
				EditorPrefs.SetBool(editorPref,bNewValue);
				SceneView.RepaintAll();
			}
			EditorGUILayout.EndHorizontal();
		}

	}

}
