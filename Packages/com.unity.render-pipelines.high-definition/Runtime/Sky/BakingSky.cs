using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
    public class BakingSky : MonoBehaviour
    {
        [SerializeField]
        VolumeProfile m_Profile;
        [SerializeField]
        int m_BakingSkyUniqueID = 0;

        // We need to keep a reference in order to unregister it upon change.
        SkySettings m_BakingSky = null;

        List<SkySettings> m_VolumeSkyList = new List<SkySettings>();


        public VolumeProfile profile
        {
            get
            {
                return m_Profile;
            }
            set
            {
                // Changing the volume is considered a destructive operation => reset the baking sky.
                if (value != m_Profile)
                {
                    m_BakingSkyUniqueID = 0;
                }

                m_Profile = value;
            }
        }

        public int bakingSkyUniqueID
        {
            get
            {
                return m_BakingSkyUniqueID;
            }
            set
            {
                m_BakingSkyUniqueID = value;
                UpdateCurrentBakingSky();
            }
        }

        void UpdateCurrentBakingSky()
        {
            SkySettings newBakingSky = GetSkyFromIDAndVolume(m_BakingSkyUniqueID, m_Profile);

            if (newBakingSky != m_BakingSky)
            {
                SkyManager.UnRegisterBakingSky(m_BakingSky);
                if (newBakingSky != null)
                    SkyManager.RegisterBakingSky(newBakingSky);

                m_BakingSky = newBakingSky;
            }
        }

        SkySettings GetSkyFromIDAndVolume(int skyUniqueID, VolumeProfile profile)
        {
            if (profile != null && skyUniqueID != 0)
            {
                m_VolumeSkyList.Clear();
                if (m_Profile.TryGetAllSubclassOf<SkySettings>(typeof(SkySettings), m_VolumeSkyList))
                {
                    foreach (var sky in m_VolumeSkyList)
                    {
                        if (skyUniqueID == SkySettings.GetUniqueID(sky.GetType()))
                        {
                            return sky;
                        }
                    }
                }
            }

            return null;
        }

        // All actions done in this method are because Editor won't go through setters so we need to manually check consistency of our data.
        void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            // If we detect that the profile has been removed we need to reset the baking sky.
            if (m_Profile == null)
            {
                m_BakingSkyUniqueID = 0;
            }

            // If we detect that the profile has changed, we need to reset the baking sky.
            // We have to do that manually because PropertyField won't go through setters.
            if (profile != null && m_BakingSky != null)
            {
                if (!profile.components.Find(x => x == m_BakingSky))
                {
                    m_BakingSkyUniqueID = 0;
                }
            }

            UpdateCurrentBakingSky();
        }

        void OnEnable()
        {
            UpdateCurrentBakingSky();
        }

        void OnDisable()
        {
            SkyManager.UnRegisterBakingSky(m_BakingSky);
            m_BakingSky = null;
        }
    }
}
