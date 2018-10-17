using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProbeCullResults
    {
        int[] m_PlanarReflectionProbeIndices;
        PlanarReflectionProbe[] m_VisiblePlanarReflectionProbes;

        CullingGroup m_CullingGroup;
        PlanarReflectionProbe[] m_Probes;

        public int visiblePlanarReflectionProbeCount { get; private set; }
        public PlanarReflectionProbe[] visiblePlanarReflectionProbes { get { return m_VisiblePlanarReflectionProbes; } }

        internal ReflectionProbeCullResults(ReflectionSystemParameters parameters)
        {
            Assert.IsTrue(parameters.maxPlanarReflectionProbePerCamera >= 0, "Maximum number of planar reflection probe must be positive");

            visiblePlanarReflectionProbeCount = 0;

            m_PlanarReflectionProbeIndices = new int[parameters.maxPlanarReflectionProbePerCamera];
            m_VisiblePlanarReflectionProbes = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbePerCamera];
        }

        public void CullPlanarReflectionProbes(CullingGroup cullingGroup, PlanarReflectionProbe[] planarReflectionProbes)
        {
            visiblePlanarReflectionProbeCount = cullingGroup.QueryIndices(true, m_PlanarReflectionProbeIndices, 0);
            for (var i = 0; i < visiblePlanarReflectionProbeCount; ++i)
                m_VisiblePlanarReflectionProbes[i] = planarReflectionProbes[m_PlanarReflectionProbeIndices[i]];
        }

        public void PrepareCull(CullingGroup cullingGroup, PlanarReflectionProbe[] planarReflectionProbesArray)
        {
            Assert.IsNull(m_CullingGroup, "Culling was prepared but not used nor disposed");
            Assert.IsNull(m_Probes, "Culling was prepared but not used nor disposed");

            m_CullingGroup = cullingGroup;
            m_Probes = planarReflectionProbesArray;
        }

        public void Cull()
        {
            Assert.IsNotNull(m_CullingGroup, "Culling was not prepared, please prepare cull before performing it.");
            Assert.IsNotNull(m_Probes, "Culling was not prepared, please prepare cull before performing it.");

            visiblePlanarReflectionProbeCount = m_CullingGroup.QueryIndices(true, m_PlanarReflectionProbeIndices, 0);
            for (var i = 0; i < visiblePlanarReflectionProbeCount; ++i)
                m_VisiblePlanarReflectionProbes[i] = m_Probes[m_PlanarReflectionProbeIndices[i]];

            CullingGroupManager.instance.Free(m_CullingGroup);
            m_CullingGroup = null;
            m_Probes = null;
        }
    }
}
