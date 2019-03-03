using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXDataMesh : VFXData
    {
        public Shader shader;

        public override VFXDataType type { get { return VFXDataType.kMesh; } }

        public override void OnEnable()
        {
            base.OnEnable();
            if (shader == null) shader = VFXResources.defaultResources.shader;
        }

        public override void CopySettings<T>(T dst)
        {
            VFXDataMesh other = dst as VFXDataMesh;
            if (other != null)
                other.shader = shader;
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.GPU;
        }

        public override bool CanBeCompiled()
        {
            return shader != null && m_Owners.Count == 1;
        }

        public override void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            Dictionary<VFXData, int> attributeBuffer,
            Dictionary<VFXData, int> eventBuffer)
        {
            var context = m_Owners[0];
            var contextData = contextToCompiledData[context];

            var mappings = new List<VFXMapping>();
            foreach (var uniform in contextData.uniformMapper.uniforms.Concat(contextData.uniformMapper.textures))
            {
                int exprIndex = expressionGraph.GetFlattenedIndex(uniform);
                foreach (var name in contextData.uniformMapper.GetNames(uniform))
                    mappings.Add(new VFXMapping(name, exprIndex));
            }

            var task = new VFXEditorTaskDesc()
            {
                externalProcessor = shader,
                values = mappings.ToArray(),
                parameters = contextData.parameters,
                type = VFXTaskType.Output
            };

            mappings.Clear();
            var mapper = contextData.cpuMapper;

            // TODO Factorize that
            var meshExp = mapper.FromNameAndId("mesh", -1);
            var transformExp = mapper.FromNameAndId("transform", -1);
            var subMaskExp = mapper.FromNameAndId("subMeshMask", -1);

            int meshIndex = meshExp != null ? expressionGraph.GetFlattenedIndex(meshExp) : -1;
            int transformIndex = transformExp != null ? expressionGraph.GetFlattenedIndex(transformExp) : -1;
            int subMaskIndex = subMaskExp != null ? expressionGraph.GetFlattenedIndex(subMaskExp) : -1;

            if (meshIndex != -1)
                mappings.Add(new VFXMapping("mesh", meshIndex));
            if (transformIndex != -1)
                mappings.Add(new VFXMapping("transform", transformIndex));
            if (subMaskIndex != -1)
                mappings.Add(new VFXMapping("subMeshMask", subMaskIndex));

            outSystemDescs.Add(new VFXEditorSystemDesc()
            {
                tasks = new VFXEditorTaskDesc[1] { task },
                values = mappings.ToArray(),
                type = VFXSystemType.Mesh,
                layer = uint.MaxValue,
            });
        }

        public override void GenerateAttributeLayout()
        {
        }

        public override string GetAttributeDataDeclaration(VFXAttributeMode mode)
        {
            throw new NotImplementedException();
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location)
        {
            throw new NotImplementedException();
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            throw new NotImplementedException();
        }
    }
}
