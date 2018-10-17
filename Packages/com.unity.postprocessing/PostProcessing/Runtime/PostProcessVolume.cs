using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    //
    // Here's a quick look at the architecture of this framework and how it's integrated into Unity
    // (written between versions 5.6 and 2017.1):
    //
    // Users have to be able to plug in their own effects without having to modify the codebase and
    // these custom effects should work out-of-the-box with all the other features we provide
    // (volume blending etc). This relies on heavy use of polymorphism, but the only way to get
    // the serialization system to work well with polymorphism in Unity is to use ScriptableObjects.
    //
    // Users can push their custom effects at different (hardcoded) injection points.
    //
    // Each effect consists of at least two classes (+ shaders): a POD "Settings" class which only
    // stores parameters, and a "Renderer" class that holds the rendering logic. Settings are linked
    // to renderers using a PostProcessAttribute. These are automatically collected at init time
    // using reflection. Settings in this case are ScriptableObjects, we only need to serialize
    // these.
    //
    // We could store these settings object straight into each volume and call it a day, but
    // unfortunately there's one feature of Unity that doesn't work well with scene-stored assets:
    // prefabs. So we need to store all of these settings in a disk-asset and treat them as
    // sub-assets.
    //
    // Note: We have to use ScriptableObject for everything but these don't work with the Animator
    //       tool. It's unfortunate but it's the only way to make it easily extensible. On the other
    //       hand, users can animate post-processing effects using Volumes or straight up scripting.
    //
    // Volume blending leverages the physics system for distance checks to the nearest point on
    // volume colliders. Each volume can have several colliders or any type (cube, mesh...), making
    // it quite a powerful feature to use.
    //
    // Volumes & blending are handled by a singleton manager (see PostProcessManager).
    //
    // Rendering is handled by a PostProcessLayer component living on the camera, which mean you
    // can easily toggle post-processing on & off or change the anti-aliasing type per-camera,
    // which is very useful when doing multi-layered camera rendering or any other technique that
    // involves multiple-camera setups. This PostProcessLayer component can also filters volumes
    // by layers (as in Unity layers) so you can easily choose which volumes should affect the
    // camera.
    //
    // All post-processing shaders MUST use the custom Standard Shader Library bundled with the
    // framework. The reason for that is because the codebase is meant to work without any
    // modification on the Classic Render Pipelines (Forward, Deferred...) and the upcoming
    // Scriptable Render Pipelines (HDPipe, LDPipe...). But these don't have compatible shader
    // libraries so instead of writing two code paths we chose to provide a minimalist, generic
    // Standard Library geared toward post-processing use. An added bonus to that if users create
    // their own post-processing effects using this framework, then they'll work without any
    // modification on both Classic and Scriptable Render Pipelines.
    //

    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Post-process Volume", 1001)]
    public sealed class PostProcessVolume : MonoBehaviour
    {
        // Modifying sharedProfile will change the behavior of all volumes using this profile, and
        // change profile settings that are stored in the project too
        public PostProcessProfile sharedProfile;

        [Tooltip("A global volume is applied to the whole scene.")]
        public bool isGlobal = false;
        
        [Min(0f), Tooltip("Outer distance to start blending from. A value of 0 means no blending and the volume overrides will be applied immediatly upon entry.")]
        public float blendDistance = 0f;

        [Range(0f, 1f), Tooltip("Total weight of this volume in the scene. 0 means it won't do anything, 1 means full effect.")]
        public float weight = 1f;
        
        [Tooltip("Volume priority in the stack. Higher number means higher priority. Negative values are supported.")]
        public float priority = 0f;

        // This property automatically instantiates the profile and make it unique to this volume
        // so you can safely edit it via scripting at runtime without changing the original asset
        // in the project.
        // Note that if you pass in your own profile, it is your responsability to destroy it once
        // it's not in use anymore.
        public PostProcessProfile profile
        {
            get
            {
                if (m_InternalProfile == null)
                {
                    m_InternalProfile = ScriptableObject.CreateInstance<PostProcessProfile>();

                    if (sharedProfile != null)
                    {
                        foreach (var item in sharedProfile.settings)
                        {
                            var itemCopy = Instantiate(item);
                            m_InternalProfile.settings.Add(itemCopy);
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

        internal PostProcessProfile profileRef
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

        int m_PreviousLayer;
        float m_PreviousPriority;
        List<Collider> m_TempColliders;
        PostProcessProfile m_InternalProfile;

        void OnEnable()
        {
            PostProcessManager.instance.Register(this);
            m_PreviousLayer = gameObject.layer;
            m_TempColliders = new List<Collider>();
        }

        void OnDisable()
        {
            PostProcessManager.instance.Unregister(this);
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
                PostProcessManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
                m_PreviousLayer = layer;
            }

            // Same for `priority`. We could use a property instead, but it doesn't play nice with
            // the serialization system. Using a custom Attribute/PropertyDrawer for a property is
            // possible but it doesn't work with Undo/Redo in the editor, which makes it useless.
            if (priority != m_PreviousPriority)
            {
                PostProcessManager.instance.SetLayerDirty(layer);
                m_PreviousPriority = priority;
            }
        }

        // TODO: Look into a better volume previsualization system
        void OnDrawGizmos()
        {
            var colliders = m_TempColliders;
            GetComponents(colliders);

            if (isGlobal || colliders == null)
                return;
            
#if UNITY_EDITOR
            // Can't access the UnityEditor.Rendering.PostProcessing namespace from here, so
            // we'll get the preferred color manually
            unchecked
            {
                int value = UnityEditor.EditorPrefs.GetInt("PostProcessing.Volume.GizmoColor", (int)0x8033cc1a);
                Gizmos.color = ColorUtilities.ToRGBA((uint)value);
            }
#endif

            var scale = transform.localScale;
            var invScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);

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
                    Gizmos.DrawWireCube(c.center, c.size + invScale * blendDistance * 4f);
                }
                else if (type == typeof(SphereCollider))
                {
                    var c = (SphereCollider)collider;
                    Gizmos.DrawSphere(c.center, c.radius);
                    Gizmos.DrawWireSphere(c.center, c.radius + invScale.x * blendDistance * 2f);
                }
                else if (type == typeof(MeshCollider))
                {
                    var c = (MeshCollider)collider;

                    // Only convex mesh colliders are allowed
                    if (!c.convex)
                        c.convex = true;

                    // Mesh pivot should be centered or this won't work
                    Gizmos.DrawMesh(c.sharedMesh);
                    Gizmos.DrawWireMesh(c.sharedMesh, Vector3.zero, Quaternion.identity, Vector3.one + invScale * blendDistance * 4f);
                }

                // Nothing for capsule (DrawCapsule isn't exposed in Gizmo), terrain, wheel and
                // other colliders...
            }

            colliders.Clear();
        }
    }
}
