using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    // Singleton used to tracks all existing volumes in the scene
    // TODO: Deal with 2D volumes !
    public sealed class PostProcessManager
    {
        static PostProcessManager s_Instance;

        public static PostProcessManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PostProcessManager();

                return s_Instance;
            }
        }

        const int k_MaxLayerCount = 32; // Max amount of layers available in Unity
        readonly Dictionary<int, List<PostProcessVolume>> m_SortedVolumes;
        readonly List<PostProcessVolume> m_Volumes;
        readonly Dictionary<int, bool> m_SortNeeded;
        readonly List<PostProcessEffectSettings> m_BaseSettings;
        readonly List<Collider> m_TempColliders;

        public readonly Dictionary<Type, PostProcessAttribute> settingsTypes;

        PostProcessManager()
        {
            m_SortedVolumes = new Dictionary<int, List<PostProcessVolume>>();
            m_Volumes = new List<PostProcessVolume>();
            m_SortNeeded = new Dictionary<int, bool>();
            m_BaseSettings = new List<PostProcessEffectSettings>();
            m_TempColliders = new List<Collider>(5);

            settingsTypes = new Dictionary<Type, PostProcessAttribute>();
            ReloadBaseTypes();
        }

#if UNITY_EDITOR
        // Called every time Unity recompile scripts in the editor. We need this to keep track of
        // any new custom effect the user might add to the project
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            instance.ReloadBaseTypes();
        }
#endif

        void CleanBaseTypes()
        {
            settingsTypes.Clear();

            foreach (var settings in m_BaseSettings)
                RuntimeUtilities.Destroy(settings);

            m_BaseSettings.Clear();
        }

        // This will be called only once at runtime and everytime script reload kicks-in in the
        // editor as we need to keep track of any compatible post-processing effects in the project
        void ReloadBaseTypes()
        {
            CleanBaseTypes();

            // Rebuild the base type map
            var types = RuntimeUtilities.GetAllAssemblyTypes()
                            .Where(
                                t => t.IsSubclassOf(typeof(PostProcessEffectSettings))
                                  && t.IsDefined(typeof(PostProcessAttribute), false)
                                  && !t.IsAbstract
                            );

            foreach (var type in types)
            {
                settingsTypes.Add(type, type.GetAttribute<PostProcessAttribute>());

                // Create an instance for each effect type, these will be used for the lowest
                // priority global volume as we need a default state when exiting volume ranges
                var inst = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                inst.SetAllOverridesTo(true, false);
                m_BaseSettings.Add(inst);
            }
        }

        // Gets a list of all volumes currently affecting the given layer. Results aren't sorted.
        // Volume with weight of 0 or no profile set will be skipped. Results list won't be cleared.
        public void GetActiveVolumes(PostProcessLayer layer, List<PostProcessVolume> results, bool skipDisabled = true, bool skipZeroWeight = true)
        {
            // If no trigger is set, only global volumes will have influence
            int mask = layer.volumeLayer.value;
            var volumeTrigger = layer.volumeTrigger;
            bool onlyGlobal = volumeTrigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : volumeTrigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            var volumes = GrabVolumes(mask);

            // Traverse all volumes
            foreach (var volume in volumes)
            {
                // Skip disabled volumes and volumes without any data or weight
                if ((skipDisabled && !volume.enabled) || volume.profileRef == null || (skipZeroWeight && volume.weight <= 0f))
                    continue;

                // Global volume always have influence
                if (volume.isGlobal)
                {
                    results.Add(volume);
                    continue;
                }

                if (onlyGlobal)
                    continue;

                // If volume isn't global and has no collider, skip it as it's useless
                var colliders = m_TempColliders;
                volume.GetComponents(colliders);
                if (colliders.Count == 0)
                    continue;

                // Find closest distance to volume, 0 means it's inside it
                float closestDistanceSqr = float.PositiveInfinity;

                foreach (var collider in colliders)
                {
                    if (!collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos); // 5.6-only API
                    var d = ((closestPoint - triggerPos) / 2f).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }

                colliders.Clear();
                float blendDistSqr = volume.blendDistance * volume.blendDistance;

                // Check for influence
                if (closestDistanceSqr <= blendDistSqr)
                    results.Add(volume);
            }
        }

        public PostProcessVolume GetHighestPriorityVolume(PostProcessLayer layer)
        {
            if (layer == null)
                throw new ArgumentNullException("layer");

            return GetHighestPriorityVolume(layer.volumeLayer);
        }

        public PostProcessVolume GetHighestPriorityVolume(LayerMask mask)
        {
            float highestPriority = float.NegativeInfinity;
            PostProcessVolume output = null;

            List<PostProcessVolume> volumes;
            if (m_SortedVolumes.TryGetValue(mask, out volumes))
            {
                foreach (var volume in volumes)
                {
                    if (volume.priority > highestPriority)
                    {
                        highestPriority = volume.priority;
                        output = volume;
                    }
                }
            }

            return output;
        }

        public PostProcessVolume QuickVolume(int layer, float priority, params PostProcessEffectSettings[] settings)
        {
            var gameObject = new GameObject()
            {
                name = "Quick Volume",
                layer = layer,
                hideFlags = HideFlags.HideAndDontSave
            };

            var volume = gameObject.AddComponent<PostProcessVolume>();
            volume.priority = priority;
            volume.isGlobal = true;
            var profile = volume.profile;

            foreach (var s in settings)
            {
                Assert.IsNotNull(s, "Trying to create a volume with null effects");
                profile.AddSettings(s);
            }

            return volume;
        }

        internal void SetLayerDirty(int layer)
        {
            Assert.IsTrue(layer >= 0 && layer <= k_MaxLayerCount, "Invalid layer bit");

            foreach (var kvp in m_SortedVolumes)
            {
                var mask = kvp.Key;

                if ((mask & (1 << layer)) != 0)
                    m_SortNeeded[mask] = true;
            }
        }

        internal void UpdateVolumeLayer(PostProcessVolume volume, int prevLayer, int newLayer)
        {
            Assert.IsTrue(prevLayer >= 0 && prevLayer <= k_MaxLayerCount, "Invalid layer bit");
            Unregister(volume, prevLayer);
            Register(volume, newLayer);
        }

        void Register(PostProcessVolume volume, int layer)
        {
            m_Volumes.Add(volume);

            // Look for existing cached layer masks and add it there if needed
            foreach (var kvp in m_SortedVolumes)
            {
                var mask = kvp.Key;

                if ((mask & (1 << layer)) != 0)
                    kvp.Value.Add(volume);
            }

            SetLayerDirty(layer);
        }

        internal void Register(PostProcessVolume volume)
        {
            int layer = volume.gameObject.layer;
            Register(volume, layer);
        }

        void Unregister(PostProcessVolume volume, int layer)
        {
            m_Volumes.Remove(volume);

            foreach (var kvp in m_SortedVolumes)
            {
                var mask = kvp.Key;

                // Skip layer masks this volume doesn't belong to
                if ((mask & (1 << layer)) == 0)
                    continue;

                kvp.Value.Remove(volume);
            }
        }

        internal void Unregister(PostProcessVolume volume)
        {
            int layer = volume.gameObject.layer;
            Unregister(volume, layer);
        }

        // Faster version of OverrideSettings to force replace values in the global state
        void ReplaceData(PostProcessLayer postProcessLayer)
        {
            foreach (var settings in m_BaseSettings)
            {
                var target = postProcessLayer.GetBundle(settings.GetType()).settings;
                int count = settings.parameters.Count;

                for (int i = 0; i < count; i++)
                    target.parameters[i].SetValue(settings.parameters[i]);
            }
        }

        internal void UpdateSettings(PostProcessLayer postProcessLayer, Camera camera)
        {
            // Reset to base state
            ReplaceData(postProcessLayer);

            // If no trigger is set, only global volumes will have influence
            int mask = postProcessLayer.volumeLayer.value;
            var volumeTrigger = postProcessLayer.volumeTrigger;
            bool onlyGlobal = volumeTrigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : volumeTrigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            var volumes = GrabVolumes(mask);

            // Traverse all volumes
            foreach (var volume in volumes)
            {
#if UNITY_EDITOR
                // Skip volumes that aren't in the scene currently displayed in the scene view
                if (!IsVolumeRenderedByCamera(volume, camera))
                    continue;
#endif

                // Skip disabled volumes and volumes without any data or weight
                if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                    continue;

                var settings = volume.profileRef.settings;

                // Global volume always have influence
                if (volume.isGlobal)
                {
                    postProcessLayer.OverrideSettings(settings, Mathf.Clamp01(volume.weight));
                    continue;
                }

                if (onlyGlobal)
                    continue;

                // If volume isn't global and has no collider, skip it as it's useless
                var colliders = m_TempColliders;
                volume.GetComponents(colliders);
                if (colliders.Count == 0)
                    continue;

                // Find closest distance to volume, 0 means it's inside it
                float closestDistanceSqr = float.PositiveInfinity;

                foreach (var collider in colliders)
                {
                    if (!collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos); // 5.6-only API
                    var d = ((closestPoint - triggerPos) / 2f).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }

                colliders.Clear();
                float blendDistSqr = volume.blendDistance * volume.blendDistance;

                // Volume has no influence, ignore it
                // Note: Volume doesn't do anything when `closestDistanceSqr = blendDistSqr` but
                //       we can't use a >= comparison as blendDistSqr could be set to 0 in which
                //       case volume would have total influence
                if (closestDistanceSqr > blendDistSqr)
                    continue;

                // Volume has influence
                float interpFactor = 1f;

                if (blendDistSqr > 0f)
                    interpFactor = 1f - (closestDistanceSqr / blendDistSqr);

                // No need to clamp01 the interpolation factor as it'll always be in [0;1[ range
                postProcessLayer.OverrideSettings(settings, interpFactor * Mathf.Clamp01(volume.weight));
            }
        }

        List<PostProcessVolume> GrabVolumes(LayerMask mask)
        {
            List<PostProcessVolume> list;

            if (!m_SortedVolumes.TryGetValue(mask, out list))
            {
                // New layer mask detected, create a new list and cache all the volumes that belong
                // to this mask in it
                list = new List<PostProcessVolume>();

                foreach (var volume in m_Volumes)
                {
                    if ((mask & (1 << volume.gameObject.layer)) == 0)
                        continue;

                    list.Add(volume);
                    m_SortNeeded[mask] = true;
                }

                m_SortedVolumes.Add(mask, list);
            }

            // Check sorting state
            bool sortNeeded;
            if (m_SortNeeded.TryGetValue(mask, out sortNeeded) && sortNeeded)
            {
                m_SortNeeded[mask] = false;
                SortByPriority(list);
            }

            return list;
        }

        // Custom insertion sort. First sort will be slower but after that it'll be faster than
        // using List<T>.Sort() which is also unstable by nature.
        // Sort order is ascending.
        static void SortByPriority(List<PostProcessVolume> volumes)
        {
            Assert.IsNotNull(volumes, "Trying to sort volumes of non-initialized layer");

            for (int i = 1; i < volumes.Count; i++)
            {
                var temp = volumes[i];
                int j = i - 1;

                while (j >= 0 && volumes[j].priority > temp.priority)
                {
                    volumes[j + 1] = volumes[j];
                    j--;
                }

                volumes[j + 1] = temp;
            }
        }

        static bool IsVolumeRenderedByCamera(PostProcessVolume volume, Camera camera)
        {
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            return UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(volume.gameObject, camera);
#else
            return true;
#endif
        }
    }
}
