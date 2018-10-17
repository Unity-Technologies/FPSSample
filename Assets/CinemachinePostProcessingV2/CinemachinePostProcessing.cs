// NOTE: If you are getting errors of the sort that say something like:
//     "The type or namespace name `PostProcessing' does not exist in the namespace"
// it is because the Post Processing Stack V2 module has not been installed in your project.
//
// To make the errors go away, you can either:
//   1 - Download PostProcessing V2 and install it into your project
// or
//   2 - Delete the CinemachinePostProcessingV2 folder from your project
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Cinemachine.PostFX
{
    /// <summary>
    /// This behaviour is a liaison between Cinemachine with the Post-Processing v2 module.  You must 
    /// have the Post-Processing V2 stack asset store package installed in order to use this behaviour.
    /// 
    /// As a component on the Virtual Camera, it holds
    /// a Post-Processing Profile asset that will be applied to the Unity camera whenever 
    /// the Virtual camera is live.  It also has the optional functionality of animating
    /// the Focus Distance and DepthOfField properties of the Camera State, and
    /// applying them to the current Post-Processing profile, provided that profile has a
    /// DepthOfField effect that is enabled.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode]
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    public class CinemachinePostProcessing : CinemachineExtension
    {
        [Tooltip("If checked, then the Focus Distance will be set to the distance between the camera and the LookAt target.  Requires DepthOfField effect in the Profile")]
        public bool m_FocusTracksTarget;

        [Tooltip("Offset from target distance, to be used with Focus Tracks Target.  Offsets the sharpest point away from the LookAt target.")]
        public float m_FocusOffset;

        [Tooltip("This Post-Processing profile will be applied whenever this virtual camera is live")]
        public PostProcessProfile m_Profile;

        bool mCachedProfileIsInvalid = true;
        PostProcessProfile mProfileCopy;
        public PostProcessProfile Profile { get { return mProfileCopy != null ? mProfileCopy : m_Profile; } }

        /// <summary>True if the profile is enabled and nontrivial</summary>
        public bool IsValid { get { return m_Profile != null && m_Profile.settings.Count > 0; } }

        /// <summary>Called by the editor when the shared asset has been edited</summary>
        public void InvalidateCachedProfile() { mCachedProfileIsInvalid = true; }

        void CreateProfileCopy()
        {
            DestroyProfileCopy();
            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            if (m_Profile != null)
            {
                foreach (var item in m_Profile.settings)
                {
                    var itemCopy = Instantiate(item);
                    profile.settings.Add(itemCopy);
                }
            }
            mProfileCopy = profile;
            mCachedProfileIsInvalid = false;
        }

        void DestroyProfileCopy()
        {
            if (mProfileCopy != null)
                RuntimeUtility.DestroyObject(mProfileCopy);
            mProfileCopy = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyProfileCopy();
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("CinemachinePostProcessing.PostPipelineStageCallback");
            // Set the focus after the camera has been fully positioned.
            // GML todo: what about collider?
            if (stage == CinemachineCore.Stage.Aim)
            {
                if (!IsValid)
                    DestroyProfileCopy();
                else
                {
                    // Handle Follow Focus
                    if (!m_FocusTracksTarget || !state.HasLookAt)
                        DestroyProfileCopy();
                    else
                    {
                        if (mProfileCopy == null || mCachedProfileIsInvalid)
                            CreateProfileCopy();
                        DepthOfField dof;
                        if (mProfileCopy.TryGetSettings(out dof))
                            dof.focusDistance.value 
                                = (state.FinalPosition - state.ReferenceLookAt).magnitude + m_FocusOffset;
                    }

                    // Apply the post-processing
                    state.AddCustomBlendable(new CameraState.CustomBlendable(this, 1));
                }
            }
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        static void OnCameraCut(CinemachineBrain brain)
        {
            // Debug.Log("Camera cut event");
            PostProcessLayer postFX = brain.PostProcessingComponent as PostProcessLayer;
            if (postFX == null)
                brain.PostProcessingComponent = null;   // object deleted
            else
                postFX.ResetHistory();
        }

        static void ApplyPostFX(CinemachineBrain brain)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("CinemachinePostProcessing.ApplyPostFX");
            PostProcessLayer ppLayer = brain.PostProcessingComponent as PostProcessLayer;
            if (ppLayer == null || !ppLayer.enabled  || ppLayer.volumeLayer == 0)
                return;

            CameraState state = brain.CurrentCameraState;
            int numBlendables = state.NumCustomBlendables;
            List<PostProcessVolume> volumes = GetDynamicBrainVolumes(brain, ppLayer, numBlendables);
            for (int i = 0; i < volumes.Count; ++i)
            {
                volumes[i].weight = 0;
                volumes[i].sharedProfile = null;
                volumes[i].profile = null;
            }
            PostProcessVolume firstVolume = null;
            int numPPblendables = 0;
            for (int i = 0; i < numBlendables; ++i)
            {
                var b = state.GetCustomBlendable(i);
                CinemachinePostProcessing src = b.m_Custom as CinemachinePostProcessing;
                if (!(src == null)) // in case it was deleted
                {
                    PostProcessVolume v = volumes[i];
                    if (firstVolume == null)
                        firstVolume = v;
                    v.sharedProfile = src.Profile;
                    v.isGlobal = true;
                    v.priority = float.MaxValue-(numBlendables-i)-1;
                    v.weight = b.m_Weight;
                    ++numPPblendables;
                }
#if true // set this to true to force first weight to 1
                // If more than one volume, then set the frst one's weight to 1
                if (numPPblendables > 1)
                    firstVolume.weight = 1;
#endif
            }
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        static string sVolumeOwnerName = "__CMVolumes";
        static  List<PostProcessVolume> sVolumes = new List<PostProcessVolume>();
        static List<PostProcessVolume> GetDynamicBrainVolumes(
            CinemachineBrain brain, PostProcessLayer ppLayer, int minVolumes)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("CinemachinePostProcessing.GetDynamicBrainVolumes");
            // Locate the camera's child object that holds our dynamic volumes
            GameObject volumeOwner = null;
            Transform t = brain.transform;
            int numChildren = t.childCount;

            sVolumes.Clear();
            for (int i = 0; volumeOwner == null && i < numChildren; ++i)
            {
                GameObject child = t.GetChild(i).gameObject;
                if (child.hideFlags == HideFlags.HideAndDontSave)
                {
                    child.GetComponents(sVolumes);
                    if (sVolumes.Count > 0)
                        volumeOwner = child;
                }
            }

            if (minVolumes > 0)
            {
                if (volumeOwner == null)
                {
                    volumeOwner = new GameObject(sVolumeOwnerName);
                    volumeOwner.hideFlags = HideFlags.HideAndDontSave;
                    volumeOwner.transform.parent = t;
                }
                // Update the volume's layer so it will be seen
                int mask = ppLayer.volumeLayer.value;
                for (int i = 0; i < 32; ++i)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        volumeOwner.layer = i;
                        break;
                    }
                }
                while (sVolumes.Count < minVolumes)
                    sVolumes.Add(volumeOwner.gameObject.AddComponent<PostProcessVolume>());
            }
            //UnityEngine.Profiling.Profiler.EndSample();
            return sVolumes;
        }

        static void StaticPostFXHandler(CinemachineBrain brain)
        {
            PostProcessLayer postFX = brain.PostProcessingComponent as PostProcessLayer;
            if (postFX == null)
            {
                brain.PostProcessingComponent = brain.GetComponent<PostProcessLayer>();
                postFX = brain.PostProcessingComponent as PostProcessLayer;
                if (postFX != null)
                        brain.m_CameraCutEvent.AddListener(CinemachinePostProcessing.OnCameraCut);
            }
            if (postFX != null)
                CinemachinePostProcessing.ApplyPostFX(brain);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        class EditorInitialize { static EditorInitialize() { InitializeModule(); } }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule()
        {
            // Afetr the brain pushes the state to the camera, hook in to the PostFX
            CinemachineCore.CameraUpdatedEvent.RemoveListener(StaticPostFXHandler);
            CinemachineCore.CameraUpdatedEvent.AddListener(StaticPostFXHandler);
        }
    }
}
