using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public class DiffusionProfileConstants
    {
        public const int DIFFUSION_PROFILE_COUNT      = 16; // Max. number of profiles, including the slot taken by the neutral profile
        public const int DIFFUSION_PROFILE_NEUTRAL_ID = 0;  // Does not result in blurring
        public const int SSS_N_SAMPLES_NEAR_FIELD     = 55; // Used for extreme close ups; must be a Fibonacci number
        public const int SSS_N_SAMPLES_FAR_FIELD      = 21; // Used at a regular distance; must be a Fibonacci number
        public const int SSS_LOD_THRESHOLD            = 4;  // The LoD threshold of the near-field kernel (in pixels)
    }

    [Serializable]
    public sealed class DiffusionProfile
    {
        public enum TexturingMode : uint
        {
            PreAndPostScatter = 0,
            PostScatter = 1
        }

        public enum TransmissionMode : uint
        {
            Regular = 0,
            ThinObject = 1
        }

        public string name;

        [ColorUsage(false, true)]
        public Color            scatteringDistance;         // Per color channel (no meaningful units)
        [ColorUsage(false, true)]
        public Color            transmissionTint;           // HDR color
        public TexturingMode    texturingMode;
        public TransmissionMode transmissionMode;
        public Vector2          thicknessRemap;             // X = min, Y = max (in millimeters)
        public float            worldScale;                 // Size of the world unit in meters
        public float            ior;                        // 1.4 for skin (mean ~0.028)

        public Vector3          shapeParam { get; private set; }               // RGB = shape parameter: S = 1 / D
        public float            maxRadius { get; private set; }                // In millimeters
        public Vector2[]        filterKernelNearField { get; private set; }    // X = radius, Y = reciprocal of the PDF
        public Vector2[]        filterKernelFarField { get; private set; }     // X = radius, Y = reciprocal of the PDF
        public Vector4          halfRcpWeightedVariances { get; private set; }
        public Vector4[]        filterKernelBasic { get; private set; }

        public DiffusionProfile(string name)
        {
            this.name          = name;

            scatteringDistance = Color.grey;
            transmissionTint   = Color.white;
            texturingMode      = TexturingMode.PreAndPostScatter;
            transmissionMode   = TransmissionMode.ThinObject;
            thicknessRemap     = new Vector2(0f, 5f);
            worldScale         = 1f;
            ior                = 1.4f; // TYpical value for skin specular reflectance
        }

        public void Validate()
        {
            thicknessRemap.y = Mathf.Max(thicknessRemap.y, 0f);
            thicknessRemap.x = Mathf.Clamp(thicknessRemap.x, 0f, thicknessRemap.y);
            worldScale       = Mathf.Max(worldScale, 0.001f);
            ior              = Mathf.Clamp(ior, 1.0f, 2.0f);

            UpdateKernel();
        }

        // Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar.
        public void UpdateKernel()
        {
            if (filterKernelNearField == null || filterKernelNearField.Length != DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD)
                filterKernelNearField = new Vector2[DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD];

            if (filterKernelFarField == null || filterKernelFarField.Length != DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD)
                filterKernelFarField = new Vector2[DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD];

            // Note: if the scattering distance is 0, exp2(-inf) will produce 0, as desired.
            shapeParam = new Vector3(1.0f / scatteringDistance.r,
                                     1.0f / scatteringDistance.g,
                                     1.0f / scatteringDistance.b);

            // We importance sample the color channel with the widest scattering distance.
            float s = Mathf.Min(shapeParam.x, shapeParam.y, shapeParam.z);

            // Importance sample the normalized diffuse reflectance profile for the computed value of 's'.
            // ------------------------------------------------------------------------------------
            // R[r, phi, s]   = s * (Exp[-r * s] + Exp[-r * s / 3]) / (8 * Pi * r)
            // PDF[r, phi, s] = r * R[r, phi, s]
            // CDF[r, s]      = 1 - 1/4 * Exp[-r * s] - 3/4 * Exp[-r * s / 3]
            // ------------------------------------------------------------------------------------

            // Importance sample the near field kernel.
            for (int i = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; i < n; i++)
            {
                float p = (i + 0.5f) * (1.0f / n);
                float r = DisneyProfileCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                filterKernelNearField[i].x = r;
                filterKernelNearField[i].y = 1f / DisneyProfilePdf(r, s);
            }

            // Importance sample the far field kernel.
            for (int i = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD; i < n; i++)
            {
                float p = (i + 0.5f) * (1.0f / n);
                float r = DisneyProfileCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                filterKernelFarField[i].x = r;
                filterKernelFarField[i].y = 1f / DisneyProfilePdf(r, s);
            }

            maxRadius = filterKernelFarField[DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD - 1].x;
        }

        static float DisneyProfile(float r, float s)
        {
            return s * (Mathf.Exp(-r * s) + Mathf.Exp(-r * s * (1.0f / 3.0f))) / (8.0f * Mathf.PI * r);
        }

        static float DisneyProfilePdf(float r, float s)
        {
            return r * DisneyProfile(r, s);
        }

        static float DisneyProfileCdf(float r, float s)
        {
            return 1.0f - 0.25f * Mathf.Exp(-r * s) - 0.75f * Mathf.Exp(-r * s * (1.0f / 3.0f));
        }

        static float DisneyProfileCdfDerivative1(float r, float s)
        {
            return 0.25f * s * Mathf.Exp(-r * s) * (1.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        static float DisneyProfileCdfDerivative2(float r, float s)
        {
            return (-1.0f / 12.0f) * s * s * Mathf.Exp(-r * s) * (3.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        // The CDF is not analytically invertible, so we use Halley's Method of root finding.
        // { f(r, s, p) = CDF(r, s) - p = 0 } with the initial guess { r = (10^p - 1) / s }.
        static float DisneyProfileCdfInverse(float p, float s)
        {
            // Supply the initial guess.
            float r = (Mathf.Pow(10f, p) - 1f) / s;
            float t = float.MaxValue;

            while (true)
            {
                float f0 = DisneyProfileCdf(r, s) - p;
                float f1 = DisneyProfileCdfDerivative1(r, s);
                float f2 = DisneyProfileCdfDerivative2(r, s);
                float dr = f0 / (f1 * (1f - f0 * f2 / (2f * f1 * f1)));

                if (Mathf.Abs(dr) < t)
                {
                    r = r - dr;
                    t = Mathf.Abs(dr);
                }
                else
                {
                    // Converged to the best result.
                    break;
                }
            }

            return r;
        }
    }

    public sealed class DiffusionProfileSettings : ScriptableObject
    {
        public DiffusionProfile[] profiles;

        [NonSerialized] public uint      texturingModeFlags;        // 1 bit/profile: 0 = PreAndPostScatter, 1 = PostScatter
        [NonSerialized] public uint      transmissionFlags;         // 1 bit/profile: 0 = regular, 1 = thin
        [NonSerialized] public Vector4[] thicknessRemaps;           // Remap: 0 = start, 1 = end - start
        [NonSerialized] public Vector4[] worldScales;               // X = meters per world unit; Y = world units per meter
        [NonSerialized] public Vector4[] shapeParams;               // RGB = S = 1 / D, A = filter radius
        [NonSerialized] public Vector4[] transmissionTintsAndFresnel0; // RGB = color, A = fresnel0
        [NonSerialized] public Vector4[] disabledTransmissionTintsAndFresnel0; // RGB = black, A = fresnel0 - For debug to remove the transmission
        [NonSerialized] public Vector4[] filterKernels;             // XY = near field, ZW = far field; 0 = radius, 1 = reciprocal of the PDF

        public DiffusionProfile this[int index]
        {
            get
            {
                if (index >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
                    throw new IndexOutOfRangeException("index");

                return profiles[index];
            }
        }

        void OnEnable()
        {
            // The neutral profile is not a part of the array.
            int profileArraySize = DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;

            if (profiles != null && profiles.Length != profileArraySize)
                Array.Resize(ref profiles, profileArraySize);

            if (profiles == null)
                profiles = new DiffusionProfile[profileArraySize];

            for (int i = 0; i < profileArraySize; i++)
            {
                if (profiles[i] == null)
                    profiles[i] = new DiffusionProfile("Profile " + (i + 1));

                profiles[i].Validate();
            }

            ValidateArray(ref thicknessRemaps,   DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref worldScales,       DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref shapeParams,       DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref transmissionTintsAndFresnel0, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref disabledTransmissionTintsAndFresnel0, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT);
            ValidateArray(ref filterKernels,     DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD);

            Debug.Assert(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT <= 32, "Transmission and Texture flags (32-bit integer) cannot support more than 32 profiles.");

            UpdateCache();
        }

        static void ValidateArray<T>(ref T[] array, int len)
        {
            if (array == null || array.Length != len)
                array = new T[len];
        }

        public void UpdateCache()
        {
            for (int i = 0; i < DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1; i++)
            {
                UpdateCache(i);
            }

            // Fill the neutral profile.
            int neutralId = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

            worldScales[neutralId] = Vector4.one;
            shapeParams[neutralId] = Vector4.zero;
            transmissionTintsAndFresnel0[neutralId].w = 0.04f; // Match DEFAULT_SPECULAR_VALUE defined in Lit.hlsl

            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
            {
                filterKernels[n * neutralId + j].x = 0f;
                filterKernels[n * neutralId + j].y = 1f;
                filterKernels[n * neutralId + j].z = 0f;
                filterKernels[n * neutralId + j].w = 1f;
            }
        }

        public void UpdateCache(int p)
        {
            // 'p' is the profile array index. 'i' is the index in the shader (accounting for the neutral profile).
            int i = p + 1;

            // Erase previous value (This need to be done here individually as in the SSS editor we edit individual component)
            uint mask = 1u << i;
            texturingModeFlags &= ~mask;
            mask = 1u << i;
            transmissionFlags &= ~mask;

            texturingModeFlags |= (uint)profiles[p].texturingMode    << i;
            transmissionFlags  |= (uint)profiles[p].transmissionMode << i;
            thicknessRemaps[i]  = new Vector4(profiles[p].thicknessRemap.x, profiles[p].thicknessRemap.y - profiles[p].thicknessRemap.x, 0f, 0f);
            worldScales[i]      = new Vector4(profiles[p].worldScale, 1.0f / profiles[p].worldScale, 0f, 0f);

            // Premultiply S by ((-1.0 / 3.0) * LOG2_E) on the CPU.
            const float log2e = 1.44269504088896340736f;
            const float k     = (-1.0f / 3.0f) * log2e;

            shapeParams[i]   = profiles[p].shapeParam * k;
            shapeParams[i].w = profiles[p].maxRadius;
            // Convert ior to fresnel0
            float fresnel0 = (profiles[p].ior - 1.0f) / (profiles[p].ior + 1.0f);
            fresnel0 *= fresnel0; // square
            transmissionTintsAndFresnel0[i] = new Vector4(profiles[p].transmissionTint.r * 0.25f, profiles[p].transmissionTint.g * 0.25f, profiles[p].transmissionTint.b * 0.25f, fresnel0); // Premultiplied
            disabledTransmissionTintsAndFresnel0[i] = new Vector4(0.0f, 0.0f, 0.0f, fresnel0);

            for (int j = 0, n = DiffusionProfileConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
            {
                filterKernels[n * i + j].x = profiles[p].filterKernelNearField[j].x;
                filterKernels[n * i + j].y = profiles[p].filterKernelNearField[j].y;

                if (j < DiffusionProfileConstants.SSS_N_SAMPLES_FAR_FIELD)
                {
                    filterKernels[n * i + j].z = profiles[p].filterKernelFarField[j].x;
                    filterKernels[n * i + j].w = profiles[p].filterKernelFarField[j].y;
                }
            }
        }
    }
}
