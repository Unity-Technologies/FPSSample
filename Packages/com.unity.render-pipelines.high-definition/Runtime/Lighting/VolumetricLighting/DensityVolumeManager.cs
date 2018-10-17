using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DensityVolumeManager
    {
        static private DensityVolumeManager _instance = null;

        public static DensityVolumeManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DensityVolumeManager();
                }
                return _instance;
            }
        }

        public Texture3DAtlas volumeAtlas = null;
        private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        public static int volumeTextureSize = 32;

        private DensityVolumeManager()
        {
            volumes = new List<DensityVolume>();

            volumeAtlas = new Texture3DAtlas(TextureFormat.Alpha8, volumeTextureSize);

            volumeAtlas.OnAtlasUpdated += AtlasUpdated;
        }

        private List<DensityVolume> volumes = null;

        public void RegisterVolume(DensityVolume volume)
        {
            volumes.Add(volume);

            volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null)
            {
                volumeAtlas.AddTexture(volume.parameters.volumeMask);
            }
        }

        public void DeRegisterVolume(DensityVolume volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
            }

            volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null)
            {
                volumeAtlas.RemoveTexture(volume.parameters.volumeMask);
            }

            //Upon removal we have to refresh the texture list.
            TriggerVolumeAtlasRefresh();
        }

        public DensityVolume[] PrepareDensityVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            //Update volumes
            bool animate = CoreUtils.AreAnimatedMaterialsEnabled(currentCam);
            foreach (DensityVolume volume in volumes)
            {
                volume.PrepareParameters(animate, time);
            }

            if (atlasNeedsRefresh)
            {
                atlasNeedsRefresh = false;
                VolumeAtlasRefresh();
            }

            volumeAtlas.GenerateAtlas(cmd);

            // GC.Alloc
            // List`1.ToArray()
            return volumes.ToArray();
        }

        private void VolumeAtlasRefresh()
        {
            volumeAtlas.ClearTextures();
            foreach (DensityVolume volume in volumes)
            {
                if (volume.parameters.volumeMask != null)
                {
                    volumeAtlas.AddTexture(volume.parameters.volumeMask);
                }
            }
        }

        public void TriggerVolumeAtlasRefresh()
        {
            atlasNeedsRefresh = true;
        }

        private void AtlasUpdated()
        {
            foreach (DensityVolume volume in volumes)
            {
                volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.volumeMask);
            }
        }
    }
}
