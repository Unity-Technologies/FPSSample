namespace UnityEngine.Experimental.Rendering
{
    [RequireComponent(typeof(Light))]
    public class AdditionalShadowData : MonoBehaviour
    {
// Currently m_Version is not used and produce a warning, remove these pragmas at the next version incrementation
#pragma warning disable 414
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("version")]
        private int m_Version = 1;
#pragma warning restore 414

        public const int DefaultShadowResolution = 512;

        public int shadowResolution = DefaultShadowResolution;

        public static int GetShadowResolution(AdditionalShadowData shadowData)
        {
            if (shadowData != null)
                return shadowData.shadowResolution;
            else
                return DefaultShadowResolution;
        }

        [Range(0.0f, 1.0f)]
        public float shadowDimmer = 1.0f;
        public float volumetricShadowDimmer = 1.0f;
        public float shadowFadeDistance = 10000.0f;
        public bool contactShadows = false;
        // bias control
        public float viewBiasMin = 0.5f;
        public float viewBiasMax = 10f;
        [Range(0.0F, 15.0F)]
        public float viewBiasScale = 1.0f;
        public float normalBiasMin = 0.2f;
        public float normalBiasMax = 4.0f;
        [Range(0.0F, 10.0F)]
        public float normalBiasScale = 1.0f;
        public bool sampleBiasScale = true;
        public bool edgeLeakFixup = false; // Causes large banding artifacts
        public bool edgeToleranceNormal = true;
        [Range(0.0F, 1.0F)]
        public float edgeTolerance = 1.0f;


        // shadow related parameters
        [System.Serializable]
        public struct ShadowData
        {
            public int format;
            public int[] data;
        };

        [HideInInspector, SerializeField]
        private int shadowCascadeCount = 4;
        [HideInInspector, SerializeField]
        private float[] shadowCascadeRatios = new float[3] { 0.05f, 0.2f, 0.3f };
        [HideInInspector, SerializeField]
        private float[] shadowCascadeBorders = new float[4] { 0.2f, 0.2f, 0.2f, 0.2f };
        [HideInInspector, SerializeField]
        private int shadowAlgorithm = 0;
        [HideInInspector, SerializeField]
        private int shadowVariant = 0;
        [HideInInspector, SerializeField]
        private int shadowPrecision = 0;
        [HideInInspector, SerializeField]
        private ShadowData shadowData = new ShadowData();
        [HideInInspector, SerializeField]
        private ShadowData[] shadowDatas = new ShadowData[0];

        public int cascadeCount { get { return shadowCascadeCount; } }
        public void GetShadowCascades(out int cascadeCount, out float[] cascadeRatios, out float[] cascadeBorders) { cascadeCount = shadowCascadeCount; cascadeRatios = shadowCascadeRatios; cascadeBorders = shadowCascadeBorders; }
        public void SetShadowCascades(int cascadeCount, float[] cascadeRatios, float[] cascadeBorders) { shadowCascadeCount = cascadeCount; shadowCascadeRatios = cascadeRatios; shadowCascadeBorders = cascadeBorders; }
        public void GetShadowAlgorithm(out int algorithm, out int variant, out int precision) { algorithm = shadowAlgorithm; variant = shadowVariant; precision = shadowPrecision; }
        public void SetShadowAlgorithm(int algorithm, int variant, int precision, int format, int[] data)
        {
            shadowAlgorithm = algorithm;
            shadowVariant = variant;
            shadowPrecision = precision;
            shadowData.format = format;
            shadowData.data = data;

            int idx = FindShadowData(format);
            if (idx < 0)
            {
                idx = shadowDatas.Length;
                ShadowData[] tmp = new ShadowData[idx + 1];
                for (int i = 0; i < idx; ++i)
                    tmp[i] = shadowDatas[i];
                shadowDatas = tmp;
            }
            shadowDatas[idx].format = format;
            shadowDatas[idx].data = data != null ? data : new int[0];
        }

        // Load a specific shadow data. Returns null if requested data is not present.
        public int[] GetShadowData(int shadowDataFormat)
        {
            if (shadowData.format == shadowDataFormat)
                return shadowData.data;

            int idx = FindShadowData(shadowDataFormat);
            return idx >= 0 ? shadowDatas[idx].data : null;
        }

        // Returns the currently set shadow data and format. Can return null.
        public int[] GetShadowData(out int shadowDataFormat)
        {
            shadowDataFormat = shadowData.format;
            return shadowData.data;
        }

#if UNITY_EDITOR
        public void CompactShadowData()
        {
            shadowDatas = new ShadowData[0];
            UnityEditor.EditorUtility.SetDirty(this);
        }

#endif
        private int FindShadowData(int shadowDataFormat)
        {
            for (int i = 0; i < shadowDatas.Length; ++i)
            {
                if (shadowDatas[i].format == shadowDataFormat)
                    return i;
            }
            return -1;
        }

        public void CopyTo(AdditionalShadowData data)
        {
            data.shadowResolution = shadowResolution;
            data.shadowDimmer = shadowDimmer;
            data.volumetricShadowDimmer = volumetricShadowDimmer;
            data.shadowFadeDistance = shadowFadeDistance;
            data.contactShadows = contactShadows;
            data.viewBiasMin = viewBiasMin;
            data.viewBiasMax = viewBiasMax;
            data.viewBiasScale = viewBiasScale;
            data.normalBiasMin = normalBiasMin;
            data.normalBiasMax = normalBiasMax;
            data.normalBiasScale = normalBiasScale;
            data.sampleBiasScale = sampleBiasScale;
            data.edgeLeakFixup = edgeLeakFixup;
            data.edgeToleranceNormal = edgeToleranceNormal;
            data.edgeTolerance = edgeTolerance;
            data.shadowCascadeCount = shadowCascadeCount;
            data.shadowCascadeRatios = new float[shadowCascadeRatios.Length];
            shadowCascadeRatios.CopyTo(data.shadowCascadeRatios, 0);
            data.shadowCascadeBorders = new float[shadowCascadeBorders.Length];
            shadowCascadeBorders.CopyTo(data.shadowCascadeBorders, 0);
            data.shadowAlgorithm = shadowAlgorithm;
            data.shadowVariant = shadowVariant;
            data.shadowPrecision = shadowPrecision;
            data.shadowData = shadowData;
            data.shadowDatas = new ShadowData[shadowDatas.Length];
            shadowDatas.CopyTo(data.shadowDatas, 0);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AdditionalShadowData))]
    [UnityEditor.CanEditMultipleObjects]
    public class AdditionalShadowDataEditor : UnityEditor.Editor
    {
#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        UnityEditor.SerializedProperty m_ShadowAlgorithm;
        UnityEditor.SerializedProperty m_ShadowVariant;
        UnityEditor.SerializedProperty m_ShadowData;
        UnityEditor.SerializedProperty m_ShadowDatas;
#pragma warning restore 414
        UnityEditor.SerializedProperty m_ShadowCascadeCount;
        UnityEditor.SerializedProperty m_ShadowCascadeRatios;
        UnityEditor.SerializedProperty m_ShadowCascadeBorders;

        void OnEnable()
        {
            m_ShadowAlgorithm = serializedObject.FindProperty("shadowAlgorithm");
            m_ShadowVariant = serializedObject.FindProperty("shadowVariant");
            m_ShadowData = serializedObject.FindProperty("shadowData");
            m_ShadowDatas = serializedObject.FindProperty("shadowDatas");
            m_ShadowCascadeCount = serializedObject.FindProperty("shadowCascadeCount");
            m_ShadowCascadeRatios = serializedObject.FindProperty("shadowCascadeRatios");
            m_ShadowCascadeBorders = serializedObject.FindProperty("shadowCascadeBorders");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            AdditionalShadowData asd = (AdditionalShadowData)target;
            if (asd == null)
                return;

            UnityEditor.EditorGUI.BeginChangeCheck();

            serializedObject.Update();

            // cascade code
            if (asd.gameObject.GetComponent<Light>().type == LightType.Directional)
            {
                UnityEditor.EditorGUI.BeginChangeCheck();
                UnityEditor.EditorGUILayout.PropertyField(m_ShadowCascadeCount);
                if (UnityEditor.EditorGUI.EndChangeCheck())
                {
                    const int kMaxCascades = 4;
                    int newcnt = m_ShadowCascadeCount.intValue <= 0 ? 1 : (m_ShadowCascadeCount.intValue > kMaxCascades ? kMaxCascades : m_ShadowCascadeCount.intValue);
                    m_ShadowCascadeCount.intValue = newcnt;
                    m_ShadowCascadeRatios.arraySize = newcnt - 1;
                    m_ShadowCascadeBorders.arraySize = newcnt;
                }
                UnityEditor.EditorGUI.indentLevel++;
                for (int i = 0; i < m_ShadowCascadeRatios.arraySize; i++)
                {
                    UnityEditor.EditorGUILayout.Slider(m_ShadowCascadeRatios.GetArrayElementAtIndex(i), 0.0f, 1.0f, new GUIContent("Cascade " + i));
                }
                for (int i = 0; i < m_ShadowCascadeBorders.arraySize; i++)
                {
                    UnityEditor.EditorGUILayout.Slider(m_ShadowCascadeBorders.GetArrayElementAtIndex(i), 0.0f, 1.0f, new GUIContent("Transition " + i));
                }
                UnityEditor.EditorGUI.indentLevel--;
            }

            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.EditorUtility.SetDirty(asd);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                UnityEditor.SceneView.RepaintAll();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
