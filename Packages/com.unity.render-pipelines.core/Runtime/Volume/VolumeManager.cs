using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering
{
    using UnityObject = UnityEngine.Object;

    public sealed class VolumeManager
    {
        //>>> System.Lazy<T> is broken in Unity (legacy runtime) so we'll have to do it ourselves :|
        static readonly VolumeManager s_Instance = new VolumeManager();
        public static VolumeManager instance { get { return s_Instance; } }

        // Explicit static constructor to tell the C# compiler not to mark type as beforefieldinit
        static VolumeManager() {}
        //<<<

        // Internal stack
        public VolumeStack stack { get; private set; }

        // Current list of tracked component types
        public IEnumerable<Type> baseComponentTypes { get; private set; }

        // Max amount of layers available in Unity
        const int k_MaxLayerCount = 32;

        // Cached lists of all volumes (sorted by priority) by layer mask
        readonly Dictionary<int, List<Volume>> m_SortedVolumes;

        // Holds all the registered volumes
        readonly List<Volume> m_Volumes;

        // Keep track of sorting states for layer masks
        readonly Dictionary<int, bool> m_SortNeeded;

        // Internal list of default state for each component type - this is used to reset component
        // states on update instead of having to implement a Reset method on all components (which
        // would be error-prone)
        readonly List<VolumeComponent> m_ComponentsDefaultState;

        // Recycled list used for volume traversal
        readonly List<Collider> m_TempColliders;

        VolumeManager()
        {
            m_SortedVolumes = new Dictionary<int, List<Volume>>();
            m_Volumes = new List<Volume>();
            m_SortNeeded = new Dictionary<int, bool>();
            m_TempColliders = new List<Collider>(8);
            m_ComponentsDefaultState = new List<VolumeComponent>();

            ReloadBaseTypes();

            stack = CreateStack();
        }

        public VolumeStack CreateStack()
        {
            var stack = new VolumeStack();
            stack.Reload(baseComponentTypes);
            return stack;
        }

        // This will be called only once at runtime and everytime script reload kicks-in in the
        // editor as we need to keep track of any compatible component in the project
        void ReloadBaseTypes()
        {
            m_ComponentsDefaultState.Clear();

            // Grab all the component types we can find
            baseComponentTypes = CoreUtils.GetAllAssemblyTypes()
                .Where(t => t.IsSubclassOf(typeof(VolumeComponent)) && !t.IsAbstract);

            // Keep an instance of each type to be used in a virtual lowest priority global volume
            // so that we have a default state to fallback to when exiting volumes
            foreach (var type in baseComponentTypes)
            {
                var inst = (VolumeComponent)ScriptableObject.CreateInstance(type);
                m_ComponentsDefaultState.Add(inst);
            }
        }

        public void Register(Volume volume, int layer)
        {
            m_Volumes.Add(volume);

            // Look for existing cached layer masks and add it there if needed
            foreach (var kvp in m_SortedVolumes)
            {
                if ((kvp.Key & (1 << layer)) != 0)
                    kvp.Value.Add(volume);
            }

            SetLayerDirty(layer);
        }

        public void Unregister(Volume volume, int layer)
        {
            m_Volumes.Remove(volume);

            foreach (var kvp in m_SortedVolumes)
            {
                // Skip layer masks this volume doesn't belong to
                if ((kvp.Key & (1 << layer)) == 0)
                    continue;

                kvp.Value.Remove(volume);
            }
        }

        public bool IsComponentActiveInMask<T>(LayerMask layerMask)
            where T : VolumeComponent
        {
            int mask = layerMask.value;

            foreach (var kvp in m_SortedVolumes)
            {
                if ((kvp.Key & mask) == 0)
                    continue;

                foreach (var volume in kvp.Value)
                {
                    if (!volume.enabled || volume.profileRef == null)
                        continue;

                    T component;
                    if (volume.profileRef.TryGet(out component) && component.active)
                        return true;
                }
            }

            return false;
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

        internal void UpdateVolumeLayer(Volume volume, int prevLayer, int newLayer)
        {
            Assert.IsTrue(prevLayer >= 0 && prevLayer <= k_MaxLayerCount, "Invalid layer bit");
            Unregister(volume, prevLayer);
            Register(volume, newLayer);
        }

        // Go through all listed components and lerp overriden values in the global state
        void OverrideData(VolumeStack stack, List<VolumeComponent> components, float interpFactor)
        {
            foreach (var component in components)
            {
                if (!component.active)
                    continue;

                var state = stack.GetComponent(component.GetType());
                component.Override(state, interpFactor);
            }
        }

        // Faster version of OverrideData to force replace values in the global state
        void ReplaceData(VolumeStack stack, List<VolumeComponent> components)
        {
            foreach (var component in components)
            {
                var target = stack.GetComponent(component.GetType());
                int count = component.parameters.Count;

                for (int i = 0; i < count; i++)
                    target.parameters[i].SetValue(component.parameters[i]);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public void CheckBaseTypes()
        {
            // Editor specific hack to work around serialization doing funky things when exiting
            if (m_ComponentsDefaultState == null || (m_ComponentsDefaultState.Count > 0 && m_ComponentsDefaultState[0] == null))
                ReloadBaseTypes();
        }

        [Conditional("UNITY_EDITOR")]
        public void CheckStack(VolumeStack stack)
        {
            // The editor doesn't reload the domain when exiting play mode but still kills every
            // object created while in play mode, like stacks' component states
            var components = stack.components;

            if (components == null)
            {
                stack.Reload(baseComponentTypes);
                return;
            }

            foreach (var kvp in components)
            {
                if (kvp.Key == null || kvp.Value == null)
                {
                    stack.Reload(baseComponentTypes);
                    return;
                }
            }
        }

        // Update the global state - should be called once per frame per transform/layer mask combo
        // in the update loop before rendering
        public void Update(Transform trigger, LayerMask layerMask)
        {
            Update(stack, trigger, layerMask);
        }

        // Update a specific stack - can be used to manage your own stack and store it for later use
        public void Update(VolumeStack stack, Transform trigger, LayerMask layerMask)
        {
            Assert.IsNotNull(stack);

            CheckBaseTypes();
            CheckStack(stack);

            // Start by resetting the global state to default values
            ReplaceData(stack, m_ComponentsDefaultState);

            bool onlyGlobal = trigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : trigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            var volumes = GrabVolumes(layerMask);

            // Traverse all volumes
            foreach (var volume in volumes)
            {
                // Skip disabled volumes and volumes without any data or weight
                if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                    continue;

                // Global volumes always have influence
                if (volume.isGlobal)
                {
                    OverrideData(stack, volume.profileRef.components, Mathf.Clamp01(volume.weight));
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

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }

                colliders.Clear();
                float blendDistSqr = volume.blendDistance * volume.blendDistance;

                // Volume has no influence, ignore it
                // Note: Volume doesn't do anything when `closestDistanceSqr = blendDistSqr` but we
                //       can't use a >= comparison as blendDistSqr could be set to 0 in which case
                //       volume would have total influence
                if (closestDistanceSqr > blendDistSqr)
                    continue;

                // Volume has influence
                float interpFactor = 1f;

                if (blendDistSqr > 0f)
                    interpFactor = 1f - (closestDistanceSqr / blendDistSqr);

                // No need to clamp01 the interpolation factor as it'll always be in [0;1[ range
                OverrideData(stack, volume.profileRef.components, interpFactor * Mathf.Clamp01(volume.weight));
            }
        }

        List<Volume> GrabVolumes(LayerMask mask)
        {
            List<Volume> list;

            if (!m_SortedVolumes.TryGetValue(mask, out list))
            {
                // New layer mask detected, create a new list and cache all the volumes that belong
                // to this mask in it
                list = new List<Volume>();

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

        // Stable insertion sort. Faster than List<T>.Sort() for our needs.
        static void SortByPriority(List<Volume> volumes)
        {
            Assert.IsNotNull(volumes, "Trying to sort volumes of non-initialized layer");

            for (int i = 1; i < volumes.Count; i++)
            {
                var temp = volumes[i];
                int j = i - 1;

                // Sort order is ascending
                while (j >= 0 && volumes[j].priority > temp.priority)
                {
                    volumes[j + 1] = volumes[j];
                    j--;
                }

                volumes[j + 1] = temp;
            }
        }
    }
}
