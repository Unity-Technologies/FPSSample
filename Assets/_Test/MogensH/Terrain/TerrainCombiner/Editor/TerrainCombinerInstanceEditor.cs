using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PocketHammer
{
    [CustomEditor(typeof(TerrainCombinerInstance))]
    public class TerrainCombinerInstanceEditor : Editor
    {
        Vector3 lastPosition;
        float lastRotation;
        Vector3 lastScale;

        public TerrainCombiner ParentCombiner
        {
            get { return CombinerInstance.transform.parent != null ? CombinerInstance.transform.parent.gameObject.GetComponent<TerrainCombiner>() : null; }
        }

        public TerrainCombinerInstance CombinerInstance
        {
            get { return (TerrainCombinerInstance) target; }
        }
        
        private void OnEnable()
        {
            lastPosition = CombinerInstance.transform.position;
            lastRotation = CombinerInstance.transform.rotation.eulerAngles.y;
            lastScale = CombinerInstance.transform.localScale;
        }

        public override void OnInspectorGUI()
        {
            if (ParentCombiner == null)
            {
                GUILayout.Label("Gameobject needs to be located as child of TerrainCombiner");
                return;
            }

            DrawDefaultInspector();

            if(GUILayout.Button("Scale to combiner"))
            {
                Vector3 scale = new Vector3
                {
                    x = ParentCombiner.WorldSize.x / CombinerInstance.SourceWorldSize.x,
                    y = CombinerInstance.transform.localScale.y,
                    z = ParentCombiner.WorldSize.z / CombinerInstance.SourceWorldSize.z
                };
                CombinerInstance.transform.localScale = scale;
                CombinerInstance.transform.localPosition = ParentCombiner.WorldSize * 0.5f;
                CombinerInstance.transform.localRotation = Quaternion.identity;
            }
            
            HandleTransformChange();
        }

        void OnSceneGUI()
        {
            if (CombinerInstance.SouceTerrain == null)
                return;

            HandleTransformChange();

            // Draw bounds
            Handles.color = Color.yellow;
            var worldSize = CombinerInstance.WorldSize;
            var cubePos = CombinerInstance.transform.position + (worldSize.y/2f - CombinerInstance.WorldGroundHeight)*Vector3.up;
            Handles.matrix = Matrix4x4.TRS(cubePos, CombinerInstance.transform.rotation, worldSize);
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
        }


        void HandleTransformChange()
        {
            

            // Contraint rotation to y axis
            Quaternion rot = CombinerInstance.transform.localRotation;
            rot = Quaternion.Euler(0, rot.eulerAngles.y, 0);
            CombinerInstance.transform.localRotation = rot;

            bool triggerRebuild = false;
            triggerRebuild |= CombinerInstance.transform.position != lastPosition;
            triggerRebuild |= CombinerInstance.transform.rotation.eulerAngles.y != lastRotation;
            triggerRebuild |= CombinerInstance.transform.localScale != lastScale;
            if (triggerRebuild)
            {
                ParentCombiner.CacheDirty = true;
                TCWorker.RequestUpdate(ParentCombiner);
            }

            lastPosition = CombinerInstance.transform.position;
            lastRotation = CombinerInstance.transform.rotation.eulerAngles.y;
            lastScale = CombinerInstance.transform.localScale;

            CombinerInstance.position.x = CombinerInstance.transform.localPosition.z / ParentCombiner.WorldSize.z;
            CombinerInstance.position.y = CombinerInstance.transform.localPosition.x  / ParentCombiner.WorldSize.x;
            CombinerInstance.rotation = CombinerInstance.transform.rotation.eulerAngles.y;
            CombinerInstance.size.x = CombinerInstance.transform.localScale.z;
            CombinerInstance.size.y = CombinerInstance.transform.localScale.x;
            
            
            // Contraint position combiner terrain height
            float y = ParentCombiner.WorldSize.y * ParentCombiner.groundLevelFraction;
            Vector3 instancePos = CombinerInstance.transform.localPosition;
            instancePos.y = y;
            CombinerInstance.transform.localPosition = instancePos;
        }
    }
}
