using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.Rendering
{
    [ExecuteInEditMode]
    public class RenderingSystemBootstrap : ComponentSystem
    {
        protected override void OnCreateManager(int capacity)
        {
            RenderPipeline.beginCameraRendering += OnBeforeCull;
            Camera.onPreCull += OnBeforeCull;
        }

        protected override void OnUpdate()
        {
        }

        [Inject]
        MeshInstanceRendererSystem m_MeshRendererSystem;

        [Inject] 
        LODGroupSystem m_LODSystem;
        
        public void OnBeforeCull(Camera camera)
        {
            
            m_LODSystem.ActiveCamera = camera;
            m_LODSystem.Update();
            m_LODSystem.ActiveCamera = null;

            
            m_MeshRendererSystem.ActiveCamera = camera;
            m_MeshRendererSystem.Update();
            m_MeshRendererSystem.ActiveCamera = null;
            
        }
    }
}