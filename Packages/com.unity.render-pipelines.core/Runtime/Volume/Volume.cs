using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteAlways]
    public class Volume : MonoBehaviour
    {
        [Tooltip("A global volume is applied to the whole scene.")]
        public bool isGlobal = false;

        [Tooltip("Volume priority in the stack. Higher number means higher priority. Negative values are supported.")]
        public float priority = 0f;

        [Tooltip("Outer distance to start blending from. A value of 0 means no blending and the volume overrides will be applied immediately upon entry.")]
        public float blendDistance = 0f;

        [Range(0f, 1f), Tooltip("Total weight of this volume in the scene. 0 means it won't do anything, 1 means full effect.")]
        public float weight = 1f;

        // Modifying sharedProfile will change the behavior of all volumes using this profile, and
        // change profile settings that are stored in the project too
        public VolumeProfile sharedProfile;

        // This property automatically instantiates the profile and makes it unique to this volume
        // so you can safely edit it via scripting at runtime without changing the original asset
        // in the project.
        // Note that if you pass in your own profile, it is your responsability to destroy it once
        // it's not in use anymore.
        public VolumeProfile profile
        {
            get
            {
                if (m_InternalProfile == null)
                {
                    m_InternalProfile = ScriptableObject.CreateInstance<VolumeProfile>();

                    if (sharedProfile != null)
                    {
                        foreach (var item in sharedProfile.components)
                        {
                            var itemCopy = Instantiate(item);
                            m_InternalProfile.components.Add(itemCopy);
                        }
                    }
                }

                return m_InternalProfile;
            }
            set
            {
                m_InternalProfile = value;
            }
        }

        internal VolumeProfile profileRef
        {
            get
            {
                return m_InternalProfile == null
                    ? sharedProfile
                    : m_InternalProfile;
            }
        }

        public bool HasInstantiatedProfile()
        {
            return m_InternalProfile != null;
        }

        // Needed for state tracking (see the comments in Update)
        int m_PreviousLayer;
        float m_PreviousPriority;
        VolumeProfile m_InternalProfile;

        void OnEnable()
        {
            m_PreviousLayer = gameObject.layer;
            VolumeManager.instance.Register(this, m_PreviousLayer);
        }

        void OnDisable()
        {
            VolumeManager.instance.Unregister(this, gameObject.layer);
        }

        void Update()
        {
            // Unfortunately we need to track the current layer to update the volume manager in
            // real-time as the user could change it at any time in the editor or at runtime.
            // Because no event is raised when the layer changes, we have to track it on every
            // frame :/
            int layer = gameObject.layer;
            if (layer != m_PreviousLayer)
            {
                VolumeManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
                m_PreviousLayer = layer;
            }

            // Same for priority. We could use a property instead, but it doesn't play nice with the
            // serialization system. Using a custom Attribute/PropertyDrawer for a property is
            // possible but it doesn't work with Undo/Redo in the editor, which makes it useless for
            // our case.
            if (priority != m_PreviousPriority)
            {
                VolumeManager.instance.SetLayerDirty(layer);
                m_PreviousPriority = priority;
            }
        }

#if UNITY_EDITOR
        // TODO: Look into a better volume previsualization system
        List<Collider> m_TempColliders;

        void OnDrawGizmos()
        {
            if (m_TempColliders == null)
                m_TempColliders = new List<Collider>();

            var colliders = m_TempColliders;
            GetComponents(colliders);

            if (isGlobal || colliders == null)
                return;

            var scale = transform.localScale;
            var invScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            Gizmos.color = new Color(0f, 1f, 0.1f, 0.35f);

            // Draw a separate gizmo for each collider
            foreach (var collider in colliders)
            {
                if (!collider.enabled)
                    continue;

                // We'll just use scaling as an approximation for volume skin. It's far from being
                // correct (and is completely wrong in some cases). Ultimately we'd use a distance
                // field or at least a tesselate + push modifier on the collider's mesh to get a
                // better approximation, but the current Gizmo system is a bit limited and because
                // everything is dynamic in Unity and can be changed at anytime, it's hard to keep
                // track of changes in an elegant way (which we'd need to implement a nice cache
                // system for generated volume meshes).
                var type = collider.GetType();

                if (type == typeof(BoxCollider))
                {
                    var c = (BoxCollider)collider;
                    Gizmos.DrawCube(c.center, c.size);
                    Gizmos.DrawWireCube(c.center, c.size + invScale * blendDistance * 2f);
                }
                else if (type == typeof(SphereCollider))
                {
                    var c = (SphereCollider)collider;
                    Gizmos.DrawSphere(c.center, c.radius);
                    Gizmos.DrawWireSphere(c.center, c.radius + invScale.x * blendDistance);
                }
                else if (type == typeof(MeshCollider))
                {
                    var c = (MeshCollider)collider;

                    // Only convex mesh colliders are allowed
                    if (!c.convex)
                        c.convex = true;

                    // Mesh pivot should be centered or this won't work
                    Gizmos.DrawMesh(c.sharedMesh);
                    Gizmos.DrawWireMesh(c.sharedMesh, Vector3.zero, Quaternion.identity, Vector3.one + invScale * blendDistance * 2f);
                }

                // Nothing for capsule (DrawCapsule isn't exposed in Gizmo), terrain, wheel and
                // other colliders...
            }

            colliders.Clear();
        }

#endif
    }
}
