using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Experimental.UIElements;

namespace PocketHammer
{
	[CustomEditor(typeof(TerrainCombiner))]
	public class TerrainCombinerEditor : Editor {

		TerrainCombiner terrainCombiner;

		bool bTriggerUpdate;

		void OnEnable () {

			terrainCombiner = (TerrainCombiner)target;

			terrainCombiner.CacheDirty = true;
			bTriggerUpdate = true;
		}

    	public override void OnInspectorGUI()
		{
			serializedObject.ApplyModifiedProperties();

			var newGroundLevelFraction = EditorGUILayout.FloatField("HeightDataGround",terrainCombiner.groundLevelFraction);
			if(newGroundLevelFraction != terrainCombiner.groundLevelFraction) {
				terrainCombiner.groundLevelFraction = Mathf.Clamp(newGroundLevelFraction,0,1);
				bTriggerUpdate = true;
			}

			if (GUILayout.Button("FORCE COMBINE")) {
				bTriggerUpdate = true;
			}

			if(bTriggerUpdate) {
				TCWorker.RequestUpdate(terrainCombiner);
				bTriggerUpdate = false;
			}
		}

        void OnSceneGUI()
        {
            // TODO this blocks for selecting other objesdt
            //if (Event.current.type == EventType.Layout)
            //    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));

            var e = Event.current;
	        var controlID = GUIUtility.GetControlID (FocusType.Passive);
	        var eventType = e.GetTypeForControl(controlID);
       
            if (eventType == EventType.MouseDown)
            {
                var combiner = (TerrainCombiner)target;
                var collider = combiner.GetComponent<TerrainCollider>();

                var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;
                if(collider.Raycast(ray, out hit, 100000))
                {
//                    Debug.DrawRay(hit.point, Vector3.up*10, Color.green, 5);

                    var instance = FindClosestInstance(hit.point);
                    if (instance != null)
                    {
	                    GUIUtility.hotControl = controlID;
                    
//	                    pendingSelectedInstance = instance;
	                    Selection.activeGameObject = instance.gameObject;
                        e.Use();
                    }
                }
            }
                
	        if (eventType == EventType.MouseUp)
	        {
		        GUIUtility.hotControl = 0;
		        e.Use();
	        }
        }


        private TerrainCombinerInstance FindClosestInstance(Vector3 worldPos)
        {
            TerrainCombinerInstance closestInstance = null;
            float closestDist = float.MaxValue;
            foreach(var instance in terrainCombiner.Instances)
            {
                Vector3 localPos = instance.transform.InverseTransformPoint(worldPos);
                Vector3 size = instance.WorldSize;
                Rect rect = new Rect(-size.x * 0.5f, -size.z * 0.5f, size.x, size.x);

                if(rect.Contains(new Vector2(localPos.x, localPos.z)))
                {
                    float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(instance.transform.position.x, instance.transform.position.z));
                    if(dist < closestDist)
                    {
                        closestDist = dist;
                        closestInstance = instance;
                    }
                }
            }

            return closestInstance;
        }
	}
	

//        private void FloatField(string name, SerializedProperty property ) {
//			GUILayout.BeginHorizontal();
//			GUILayout.Label(name, GUILayout.Width( 75 ));
//			GUILayout.FlexibleSpace();
//			EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.MinWidth( 50 ) );
//			GUILayout.EndHorizontal();
//		}
//
//		private void Vector2Field(string name, SerializedProperty property ) {
//			GUILayout.BeginHorizontal();
//			GUILayout.Label(name, GUILayout.Width( 75 ));
//			GUILayout.FlexibleSpace();
//			EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.MinWidth( 100 ) );
//			GUILayout.EndHorizontal();
//		}	
	
	
//		public static string FindAssetFolder(string folderToStart, string desiredFolderName)
//		{
//			string[] folderEntries = Directory.GetDirectories(folderToStart);
//
//			for (int n = 0, len = folderEntries.Length; n < len; ++n)
//			{
//				string folderName = GetLastFolder(folderEntries[n]);
//				//Debug.Log("folderName: " + folderName);
//
//				if (folderName == desiredFolderName)
//				{
//					return folderEntries[n];
//				}
//				else
//				{
//					string recursed = FindAssetFolder(folderEntries[n], desiredFolderName);
//					string recursedFolderName = GetLastFolder(recursed);
//					if (recursedFolderName == desiredFolderName)
//					{
//						return recursed;
//					}
//				}
//			}
//			return "";
//		}

//		static string GetLastFolder(string inFolder)
//		{
//			inFolder = inFolder.Replace('\\', '/');
//
//			//Debug.Log("folder: " + inFolder);
//			//string folderName = Path.GetDirectoryName(folderEntries[n]);
//
//			int lastSlashIdx = inFolder.LastIndexOf('/');
//			if (lastSlashIdx == -1)
//			{
//				return "";
//			}
//			return inFolder.Substring(lastSlashIdx+1, inFolder.Length-lastSlashIdx-1);
//		}
}
