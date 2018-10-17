using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MasterNode<T> : AbstractMaterialNode, IMasterNode, IHasSettings
        where T : class, ISubShader
    {
        [NonSerialized]
        List<T> m_SubShaders = new List<T>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSubShaders = new List<SerializationHelper.JSONSerializedElement>();

        public override bool hasPreview
        {
            get { return true; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public Type supportedSubshaderType
        {
            get { return typeof(T); }
        }

        public IEnumerable<T> subShaders
        {
            get { return m_SubShaders; }
        }

        public void AddSubShader(T subshader)
        {
            if (m_SubShaders.Contains(subshader))
                return;

            m_SubShaders.Add(subshader);
            Dirty(ModificationScope.Graph);
        }

        public void RemoveSubShader(T subshader)
        {
            m_SubShaders.RemoveAll(x => x == subshader);
            Dirty(ModificationScope.Graph);
        }

        public string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var shaderProperties = new PropertyCollector();

            var abstractMaterialGraph = owner as AbstractMaterialGraph;
            if (abstractMaterialGraph != null)
                abstractMaterialGraph.CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            var finalShader = new ShaderStringBuilder();
            finalShader.AppendLine(@"Shader ""{0}""", outputName);
            using (finalShader.BlockScope())
            {
                finalShader.AppendLine("Properties");
                using (finalShader.BlockScope())
                {
                    finalShader.AppendLine(shaderProperties.GetPropertiesBlock(0));
                }

                foreach (var subShader in m_SubShaders)
                {
                    if (mode != GenerationMode.Preview || subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                        finalShader.AppendLines(subShader.GetSubshader(this, mode, sourceAssetDependencyPaths));
                }

                finalShader.AppendLine(@"FallBack ""Hidden/InternalErrorShader""");
            }
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.ToString();
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            foreach (var subShader in m_SubShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return true;
            }
            return false;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableSubShaders = SerializationHelper.Serialize<T>(m_SubShaders);
        }

        public override void OnAfterDeserialize()
        {
            m_SubShaders = SerializationHelper.Deserialize<T>(m_SerializableSubShaders, GraphUtil.GetLegacyTypeRemapping());
            m_SubShaders.RemoveAll(x => x == null);
            m_SerializableSubShaders = null;
            base.OnAfterDeserialize();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    var isValid = !type.IsAbstract && type.IsPublic && !type.IsGenericType && type.IsClass && typeof(T).IsAssignableFrom(type);
                    if (isValid && !subShaders.Any(s => s.GetType() == type))
                    {
                        try
                        {
                            var subShader = (T)Activator.CreateInstance(type);
                            AddSubShader(subShader);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        public VisualElement CreateSettingsElement()
        {
            var container = new VisualElement();
            var commonSettingsElement = CreateCommonSettingsElement();
            if (commonSettingsElement != null)
                container.Add(commonSettingsElement);

            return container;
        }

        protected virtual VisualElement CreateCommonSettingsElement()
        {
            return null;
        }
    }
}
