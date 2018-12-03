using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShaderPropertyType
{
    Color = 0,
    Vector = 1,
    Float = 2,
    Range = 3,
    TexEnv = 4
}

[ExecuteInEditMode]
public class MaterialPropertyOverride : MonoBehaviour
{
    [System.Serializable]
    public class ShaderPropertyValue
    {

        public string propertyName;
        public ShaderPropertyType type;
        public Color colValue;
        public Vector4 vecValue;
        public float floatValue;
        public Texture texValue;
    }

    [System.Serializable]
    public class MaterialOverride
    {
        public bool active;
        [System.NonSerialized]
        public bool showAll;
        public Material material;
        public MaterialPropertyOverrideAsset propertyOverrideAsset = null;
        public List<ShaderPropertyValue> propertyOverrides = new List<ShaderPropertyValue>();
    }

    public List<MaterialOverride> materialOverrides = new List<MaterialOverride>();

    // List of renderers we are affecting
    public List<Renderer> m_Renderers = new List<Renderer>();

    // List of materials we want to touch
    public List<Material> m_Materials = new List<Material>();

    private void OnEnable()
    {
        Apply();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnValidate()
    {
        Clear();
        Apply();
    }

    // Try to do something reasonable when component is added
    private void Reset()
    {
        Clear();
        m_Renderers.Clear();
        m_Renderers.AddRange(GetComponents<Renderer>());
        if (m_Renderers.Count == 0)
        {
            // Fall back, try LODGroup
            var lg = GetComponent<LODGroup>();
            if (lg != null)
            {
                foreach (var l in lg.GetLODs())
                    m_Renderers.AddRange(l.renderers);
            }
        }
        Apply();
    }

    public void Clear()
    {
        foreach (var r in m_Renderers)
        {
            if (r == null)
                continue;
            r.SetPropertyBlock(null);
            for (int i = 0, c = r.sharedMaterials.Length; i < c; i++)
            {
                r.SetPropertyBlock(null, i);
            }

        }
    }

    public void Populate()
    {
        m_Renderers.Clear();
        var childRenderers = GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 100)
        {
            Debug.LogError("Too many renderers.");
            return;
        }
        m_Renderers.AddRange(childRenderers);
    }

    public void Apply()
    {
        // Apply overrides
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        foreach (var renderer in m_Renderers)
        {
            // Can happen when you are editing the list
            if (renderer == null)
                continue;

            if (renderer.sharedMaterials.Length == 0)
                continue;

            // Two cases.
            // A) there are only one type of material on this renderer. Then we 
            //    can use the master material property block.
            // B) different materials; then we use the per material property blocks

            bool allSame = true;

            // Check if multiple materials on this renderer
            for (int i = 1, c = renderer.sharedMaterials.Length; i < c; i++)
            {
                if (renderer.sharedMaterials[i] != renderer.sharedMaterials[0])
                    allSame = false;
            }

            if (allSame)
            {
                // Set master MaterialPropertyBlock
                mpb.Clear();
                var o = materialOverrides.Find(x => x.material == renderer.sharedMaterials[0]);
                if (o == null || o.active == false)
                {
                    renderer.SetPropertyBlock(null);
                }
                else
                {
                    if (o.propertyOverrideAsset != null)
                        ApplyOverrides(mpb, o.propertyOverrideAsset.propertyOverrides);
                    ApplyOverrides(mpb, o.propertyOverrides);
                    renderer.SetPropertyBlock(mpb);
                }
            }
            else
            {
                // Set specific MaterialPropertyBlocks
                for (int i = 0, c = renderer.sharedMaterials.Length; i < c; i++)
                {
                    var o = materialOverrides.Find(x => x.material == renderer.sharedMaterials[i]);
                    if (o == null || o.active == false)
                    {
                        renderer.SetPropertyBlock(null, i);
                    }
                    else
                    {
                        mpb.Clear();
                        if (o.propertyOverrideAsset != null)
                            ApplyOverrides(mpb, o.propertyOverrideAsset.propertyOverrides);
                        ApplyOverrides(mpb, o.propertyOverrides);
                        renderer.SetPropertyBlock(mpb, i);
                    }
                }
            }
        }
    }

    // Applies a list of individual override values to an mpb
    void ApplyOverrides(MaterialPropertyBlock mpb, List<ShaderPropertyValue> overrides)
    {
        foreach (var spv in overrides)
        {
            switch (spv.type)
            {
                case ShaderPropertyType.Color:
                    mpb.SetColor(spv.propertyName, spv.colValue);
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    mpb.SetFloat(spv.propertyName, spv.floatValue);
                    break;
                case ShaderPropertyType.Vector:
                    mpb.SetVector(spv.propertyName, spv.vecValue);
                    break;
                case ShaderPropertyType.TexEnv:
                    if (spv.texValue != null)
                        mpb.SetTexture(spv.propertyName, spv.texValue);
                    break;
            }
        }
    }
}
