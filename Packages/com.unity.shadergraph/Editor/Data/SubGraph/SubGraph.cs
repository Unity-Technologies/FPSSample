using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SubGraph : AbstractMaterialGraph
        , IGeneratesBodyCode
        , IGeneratesFunction
    {
        [NonSerialized]
        private SubGraphOutputNode m_OutputNode;

        public SubGraphOutputNode outputNode
        {
            get
            {
                // find existing node
                if (m_OutputNode == null)
                    m_OutputNode = GetNodes<SubGraphOutputNode>().FirstOrDefault();

                return m_OutputNode;
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_OutputNode = null;
        }

        public override void AddNode(INode node)
        {
            var materialNode = node as AbstractMaterialNode;
            if (materialNode != null)
            {
                var amn = materialNode;
                if (!amn.allowedInSubGraph)
                {
                    Debug.LogWarningFormat("Attempting to add {0} to Sub Graph. This is not allowed.", amn.GetType());
                    return;
                }
            }
            base.AddNode(node);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            foreach (var node in activeNodes)
            {
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(visitor, graphContext, generationMode);
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            foreach (var node in activeNodes)
            {
                node.ValidateNode();
                if (node is IGeneratesFunction)
                    (node as IGeneratesFunction).GenerateNodeFunction(registry, graphContext, generationMode);
            }
        }

        public IEnumerable<IShaderProperty> graphInputs
        {
            get { return properties.OrderBy(x => x.guid); }
        }

        public IEnumerable<MaterialSlot> graphOutputs
        {
            get
            {
                return outputNode != null ? outputNode.graphOutputs : new List<MaterialSlot>();
            }
        }

        public void GenerateSubGraphFunction(string functionName, FunctionRegistry registry, GraphContext graphContext, ShaderGraphRequirements reqs, GenerationMode generationMode)
        {
            registry.ProvideFunction(functionName, s =>
                {
                    s.AppendLine("// Subgraph function");

                    // Generate arguments... first INPUTS
                    var arguments = new List<string>();
                    foreach (var prop in graphInputs)
                        arguments.Add(string.Format("{0}", prop.GetPropertyAsArgumentString()));

                    // now pass surface inputs
                    arguments.Add(string.Format("{0} IN", graphContext.graphInputStructName));

                    // Now generate outputs
                    foreach (var slot in graphOutputs)
                        arguments.Add(string.Format("out {0} {1}", slot.concreteValueType.ToString(outputNode.precision), slot.shaderOutputName));

                    // Create the function protoype from the arguments
                    s.AppendLine("void {0}({1})"
                        , functionName
                        , arguments.Aggregate((current, next) => string.Format("{0}, {1}", current, next)));

                    // now generate the function
                    using (s.BlockScope())
                    {
                        // Just grab the body from the active nodes
                        var bodyGenerator = new ShaderGenerator();
                        GenerateNodeCode(bodyGenerator, graphContext, generationMode);

                        if (outputNode != null)
                            outputNode.RemapOutputs(bodyGenerator, generationMode);

                        s.Append(bodyGenerator.GetShaderString(1));
                    }
                });
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if we are previewing the graph we need to
            // export 'exposed props' if we are 'for real'
            // then we are outputting the graph in the
            // nested context and the needed values will
            // be copied into scope.
            if (generationMode == GenerationMode.Preview)
            {
                foreach (var prop in properties)
                    collector.AddShaderProperty(prop);
            }

            foreach (var node in activeNodes)
            {
                if (node is IGenerateProperties)
                    (node as IGenerateProperties).CollectShaderProperties(collector, generationMode);
            }
        }

        public IEnumerable<PreviewProperty> GetPreviewProperties()
        {
            List<PreviewProperty> props = new List<PreviewProperty>();
            foreach (var node in activeNodes)
                node.CollectPreviewMaterialProperties(props);
            return props;
        }

        public IEnumerable<AbstractMaterialNode> activeNodes
        {
            get
            {
                List<INode> nodes = new List<INode>();
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, outputNode);
                return nodes.OfType<AbstractMaterialNode>();
            }
        }
    }
}
