using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PreviewManager : IDisposable
    {
        AbstractMaterialGraph m_Graph;
        List<PreviewRenderData> m_RenderDatas = new List<PreviewRenderData>();
        PreviewRenderData m_MasterRenderData;
        List<Identifier> m_Identifiers = new List<Identifier>();
        IndexSet m_DirtyPreviews = new IndexSet();
        IndexSet m_DirtyShaders = new IndexSet();
        IndexSet m_TimeDependentPreviews = new IndexSet();
        Material m_PreviewMaterial;
        MaterialPropertyBlock m_PreviewPropertyBlock;
        PreviewSceneResources m_SceneResources;
        Texture2D m_ErrorTexture;
        Shader m_ColorShader;
        string m_OutputIdName;
        Vector2? m_NewMasterPreviewSize;

        public PreviewRenderData masterRenderData
        {
            get { return m_MasterRenderData; }
        }

        public PreviewManager(AbstractMaterialGraph graph)
        {
            m_Graph = graph;
            m_PreviewMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideInHierarchy };
            m_PreviewMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_PreviewPropertyBlock = new MaterialPropertyBlock();
            m_ErrorTexture = new Texture2D(2, 2);
            m_ErrorTexture.SetPixel(0, 0, Color.magenta);
            m_ErrorTexture.SetPixel(0, 1, Color.black);
            m_ErrorTexture.SetPixel(1, 0, Color.black);
            m_ErrorTexture.SetPixel(1, 1, Color.magenta);
            m_ErrorTexture.filterMode = FilterMode.Point;
            m_ErrorTexture.Apply();
            m_SceneResources = new PreviewSceneResources();
            m_ColorShader = ShaderUtil.CreateShaderAsset(k_EmptyShader);
            m_ColorShader.hideFlags = HideFlags.HideAndDontSave;
            m_MasterRenderData = new PreviewRenderData
            {
                renderTexture = new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave }
            };
            m_MasterRenderData.renderTexture.Create();

            foreach (var node in m_Graph.GetNodes<INode>())
                AddPreview(node);
        }

        public void ResizeMasterPreview(Vector2 newSize)
        {
            m_NewMasterPreviewSize = newSize;
            if (masterRenderData.shaderData != null)
                m_DirtyPreviews.Add(masterRenderData.shaderData.node.tempId.index);
        }

        public PreviewRenderData GetPreview(AbstractMaterialNode node)
        {
            return m_RenderDatas[node.tempId.index];
        }

        void AddPreview(INode node)
        {
            var shaderData = new PreviewShaderData
            {
                node = node
            };
            var renderData = new PreviewRenderData
            {
                shaderData = shaderData,
                renderTexture = new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave }
            };
            renderData.renderTexture.Create();
            Set(m_Identifiers, node.tempId, node.tempId);
            Set(m_RenderDatas, node.tempId, renderData);
            m_DirtyShaders.Add(node.tempId.index);
            node.RegisterCallback(OnNodeModified);
            if (node.RequiresTime())
                m_TimeDependentPreviews.Add(node.tempId.index);

            var masterNode = node as IMasterNode;

            if (masterRenderData.shaderData == null && masterNode != null)
                masterRenderData.shaderData = shaderData;

            var subGraphOutputNode = node as SubGraphOutputNode;

            if (masterRenderData.shaderData == null && subGraphOutputNode != null)
                masterRenderData.shaderData = shaderData;
        }

        void OnNodeModified(INode node, ModificationScope scope)
        {
            if (scope >= ModificationScope.Graph)
                m_DirtyShaders.Add(node.tempId.index);
            else if (scope == ModificationScope.Node)
                m_DirtyPreviews.Add(node.tempId.index);

            if (node.RequiresTime())
                m_TimeDependentPreviews.Add(node.tempId.index);
            else
                m_TimeDependentPreviews.Remove(node.tempId.index);
        }

        Stack<Identifier> m_Wavefront = new Stack<Identifier>();
        List<IEdge> m_Edges = new List<IEdge>();
        List<MaterialSlot> m_Slots = new List<MaterialSlot>();

        void PropagateNodeSet(IndexSet nodeSet, bool forward = true, IEnumerable<Identifier> initialWavefront = null)
        {
            m_Wavefront.Clear();
            if (initialWavefront != null)
            {
                foreach (var id in initialWavefront)
                    m_Wavefront.Push(id);
            }
            else
            {
                foreach (var index in nodeSet)
                    m_Wavefront.Push(m_Identifiers[index]);
            }
            while (m_Wavefront.Count > 0)
            {
                var index = m_Wavefront.Pop();
                var node = m_Graph.GetNodeFromTempId(index);
                if (node == null)
                    continue;

                // Loop through all nodes that the node feeds into.
                m_Slots.Clear();
                if (forward)
                    node.GetOutputSlots(m_Slots);
                else
                    node.GetInputSlots(m_Slots);
                foreach (var slot in m_Slots)
                {
                    m_Edges.Clear();
                    m_Graph.GetEdges(slot.slotReference, m_Edges);
                    foreach (var edge in m_Edges)
                    {
                        // We look at each node we feed into.
                        var connectedSlot = forward ? edge.inputSlot : edge.outputSlot;
                        var connectedNodeGuid = connectedSlot.nodeGuid;
                        var connectedNode = m_Graph.GetNodeFromGuid(connectedNodeGuid);

                        // If the input node is already in the set of time-dependent nodes, we don't need to process it.
                        if (nodeSet.Contains(connectedNode.tempId.index))
                            continue;

                        // Add the node to the set of time-dependent nodes, and to the wavefront such that we can process the nodes that it feeds into.
                        nodeSet.Add(connectedNode.tempId.index);
                        m_Wavefront.Push(connectedNode.tempId);
                    }
                }
            }
        }

        public void HandleGraphChanges()
        {
            foreach (var node in m_Graph.removedNodes)
                DestroyPreview(node.tempId);

            foreach (var node in m_Graph.addedNodes)
                AddPreview(node);

            foreach (var edge in m_Graph.removedEdges)
            {
                var node = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                if (node != null)
                    m_DirtyShaders.Add(node.tempId.index);
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var node = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                if (node != null)
                    m_DirtyShaders.Add(node.tempId.index);
            }
        }

        List<PreviewProperty> m_PreviewProperties = new List<PreviewProperty>();
        List<PreviewRenderData> m_RenderList2D = new List<PreviewRenderData>();
        List<PreviewRenderData> m_RenderList3D = new List<PreviewRenderData>();
        IndexSet m_PropertyNodes = new IndexSet();

        public void RenderPreviews()
        {
            m_NodesWith3DPreview.Clear();
            m_NodesWithWireframePreview.Clear();
            foreach (var node in m_Graph.GetNodes<AbstractMaterialNode>())
            {
                if (node.previewMode == PreviewMode.Preview3D)
                    m_NodesWith3DPreview.Add(node.tempId.index);
                else if (node.previewMode == PreviewMode.Wireframe)
                    m_NodesWithWireframePreview.Add(node.tempId.index);
            }
            PropagateNodeSet(m_NodesWith3DPreview);
            PropagateNodeSet(m_NodesWithWireframePreview);
            m_NodesWith3DPreview.UnionWith(m_NodesWithWireframePreview);

            UpdateShaders();

            // Union time dependent previews into dirty previews
            m_DirtyPreviews.UnionWith(m_TimeDependentPreviews);
            PropagateNodeSet(m_DirtyPreviews);

            foreach (var index in m_DirtyPreviews)
            {
                var renderData = m_RenderDatas[index];
                renderData.previewMode = m_NodesWith3DPreview.Contains(renderData.shaderData.node.tempId.index) ? PreviewMode.Preview3D : PreviewMode.Preview2D;
            }

            // Find nodes we need properties from
            m_PropertyNodes.Clear();
            m_PropertyNodes.UnionWith(m_DirtyPreviews);
            PropagateNodeSet(m_PropertyNodes, false);

            // Fill MaterialPropertyBlock
            m_PreviewPropertyBlock.Clear();
            m_PreviewPropertyBlock.SetFloat(m_OutputIdName, -1);
            foreach (var index in m_PropertyNodes)
            {
                var node = m_Graph.GetNodeFromTempId(m_Identifiers[index]) as AbstractMaterialNode;
                if (node == null)
                    continue;
                node.CollectPreviewMaterialProperties(m_PreviewProperties);
                foreach (var prop in m_Graph.properties)
                    m_PreviewProperties.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in m_PreviewProperties)
                    m_PreviewPropertyBlock.SetPreviewProperty(previewProperty);
                m_PreviewProperties.Clear();
            }

            foreach (var i in m_DirtyPreviews)
            {
                var renderData = m_RenderDatas[i];
                if (renderData.shaderData.shader == null)
                {
                    renderData.texture = null;
                    continue;
                }
                if (renderData.shaderData.hasError)
                {
                    renderData.texture = m_ErrorTexture;
                    continue;
                }

                if (renderData.previewMode == PreviewMode.Preview2D)
                    m_RenderList2D.Add(renderData);
                else
                    m_RenderList3D.Add(renderData);
            }

            m_RenderList3D.Sort((data1, data2) => data1.shaderData.shader.GetInstanceID().CompareTo(data2.shaderData.shader.GetInstanceID()));
            m_RenderList2D.Sort((data1, data2) => data1.shaderData.shader.GetInstanceID().CompareTo(data2.shaderData.shader.GetInstanceID()));

            var time = Time.realtimeSinceStartup;
            EditorUtility.SetCameraAnimateMaterialsTime(m_SceneResources.camera, time);

            m_SceneResources.light0.enabled = true;
            m_SceneResources.light0.intensity = 1.0f;
            m_SceneResources.light0.transform.rotation = Quaternion.Euler(50f, 50f, 0);
            m_SceneResources.light1.enabled = true;
            m_SceneResources.light1.intensity = 1.0f;
            m_SceneResources.camera.clearFlags = CameraClearFlags.Depth;

            // Render 2D previews
            m_SceneResources.camera.transform.position = -Vector3.forward * 2;
            m_SceneResources.camera.transform.rotation = Quaternion.identity;
            m_SceneResources.camera.orthographicSize = 0.5f;
            m_SceneResources.camera.orthographic = true;

            foreach (var renderData in m_RenderList2D)
                RenderPreview(renderData, m_SceneResources.quad, Matrix4x4.identity);

            // Render 3D previews
            m_SceneResources.camera.transform.position = -Vector3.forward * 5;
            m_SceneResources.camera.transform.rotation = Quaternion.identity;
            m_SceneResources.camera.orthographic = false;

            foreach (var renderData in m_RenderList3D)
                RenderPreview(renderData, m_SceneResources.sphere, Matrix4x4.identity);

            var renderMasterPreview = masterRenderData.shaderData != null && m_DirtyPreviews.Contains(masterRenderData.shaderData.node.tempId.index);
            if (renderMasterPreview)
            {
                if (m_NewMasterPreviewSize.HasValue)
                {
                    if (masterRenderData.renderTexture != null)
                        Object.DestroyImmediate(masterRenderData.renderTexture, true);
                    masterRenderData.renderTexture = new RenderTexture((int)m_NewMasterPreviewSize.Value.x, (int)m_NewMasterPreviewSize.Value.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
                    masterRenderData.renderTexture.Create();
                    masterRenderData.texture = masterRenderData.renderTexture;
                    m_NewMasterPreviewSize = null;
                }
                var mesh = m_Graph.previewData.serializedMesh.mesh ? m_Graph.previewData.serializedMesh.mesh :  m_SceneResources.sphere;
                var previewTransform = Matrix4x4.Rotate(m_Graph.previewData.rotation);
                var scale = m_Graph.previewData.scale;
                previewTransform *= Matrix4x4.Scale(scale * Vector3.one * (Vector3.one).magnitude / mesh.bounds.size.magnitude);
                previewTransform *= Matrix4x4.Translate(-mesh.bounds.center);
                RenderPreview(masterRenderData, mesh, previewTransform);
            }

            m_SceneResources.light0.enabled = false;
            m_SceneResources.light1.enabled = false;

            foreach (var renderData in m_RenderList2D)
                renderData.NotifyPreviewChanged();
            foreach (var renderData in m_RenderList3D)
                renderData.NotifyPreviewChanged();
            if (renderMasterPreview)
                masterRenderData.NotifyPreviewChanged();

            m_RenderList2D.Clear();
            m_RenderList3D.Clear();
            m_DirtyPreviews.Clear();
        }

        IndexSet m_NodesWith3DPreview = new IndexSet();
        IndexSet m_NodesWithWireframePreview = new IndexSet();

        void UpdateShaders()
        {
            if (m_DirtyShaders.Any())
            {
                PropagateNodeSet(m_DirtyShaders);

                var masterNodes = new List<INode>();
                var colorNodes = new List<INode>();
                var wireframeNodes = new List<INode>();
                foreach (var index in m_DirtyShaders)
                {
                    var node = m_Graph.GetNodeFromTempId(m_Identifiers[index]) as AbstractMaterialNode;
                    if (node == null)
                        continue;
                    var masterNode = node as IMasterNode;
                    if (masterNode != null)
                        masterNodes.Add(node);
                    else if (node.previewMode == PreviewMode.Wireframe)
                        wireframeNodes.Add(node);
                    else
                        colorNodes.Add(node);
                }
                var count = Math.Min(colorNodes.Count, 1) + masterNodes.Count;

                try
                {
                    var i = 0;
                    EditorUtility.DisplayProgressBar("Shader Graph", string.Format("Compiling preview shaders ({0}/{1})", i, count), 0f);
                    foreach (var node in masterNodes)
                    {
                        UpdateShader(node.tempId);
                        i++;
                        EditorUtility.DisplayProgressBar("Shader Graph", string.Format("Compiling preview shaders ({0}/{1})", i, count), 0f);
                    }
                    if (colorNodes.Count > 0)
                    {
                        var results = m_Graph.GetUberColorShader();
                        m_OutputIdName = results.outputIdProperty.referenceName;
                        ShaderUtil.UpdateShaderAsset(m_ColorShader, results.shader);
                        var debugOutputPath = DefaultShaderIncludes.GetDebugOutputPath();
                        if (debugOutputPath != null)
                            File.WriteAllText(debugOutputPath + "/ColorShader.shader", (results.shader ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));
                        bool uberShaderHasError = false;
                        if (MaterialGraphAsset.ShaderHasError(m_ColorShader))
                        {
                            var errors = MaterialGraphAsset.GetShaderErrors(m_ColorShader);
                            var message = new ShaderStringBuilder();
                            message.AppendLine(@"Preview shader for graph has {0} error{1}:", errors.Length, errors.Length != 1 ? "s" : "");
                            foreach (var error in errors)
                            {
                                INode node;
                                try
                                {
                                    node = results.sourceMap.FindNode(error.line);
                                    message.AppendLine("Shader compilation error in {3} at line {1} (on {2}):\n{0}", error.message, error.line, error.platform, node != null ? string.Format("node {0} ({1})", node.name, node.guid) : "graph");
                                    message.AppendLine(error.messageDetails);
                                    message.AppendNewLine();
                                }
                                catch
                                {
                                    message.AppendLine("Shader compilation error in {3} at line {1} (on {2}):\n{0}", error.message, error.line, error.platform, "graph");
                                }
                            }
                            Debug.LogWarning(message.ToString());
                            ShaderUtil.ClearShaderErrors(m_ColorShader);
                            ShaderUtil.UpdateShaderAsset(m_ColorShader, k_EmptyShader);
                            uberShaderHasError = true;
                        }

                        foreach (var node in colorNodes)
                        {
                            var renderData = GetRenderData(node.tempId);
                            if (renderData == null)
                                continue;
                            var shaderData = renderData.shaderData;
                            shaderData.shader = m_ColorShader;
                            shaderData.hasError = uberShaderHasError;
                        }
                        i++;
                        EditorUtility.DisplayProgressBar("Shader Graph", string.Format("Compiling preview shaders ({0}/{1})", i, count), 0f);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                // Union dirty shaders into dirty previews
                m_DirtyPreviews.UnionWith(m_DirtyShaders);
                m_DirtyShaders.Clear();
            }
        }

        void RenderPreview(PreviewRenderData renderData, Mesh mesh, Matrix4x4 transform)
        {
            m_PreviewPropertyBlock.SetFloat(m_OutputIdName, renderData.shaderData.node.tempId.index);
            if (m_PreviewMaterial.shader != renderData.shaderData.shader)
                m_PreviewMaterial.shader = renderData.shaderData.shader;
            var previousRenderTexure = RenderTexture.active;


            //Temp workaround for alpha previews...
            var temp = RenderTexture.GetTemporary(renderData.renderTexture.descriptor);
            RenderTexture.active = temp;
            GL.Clear(true, true, Color.black);
            Graphics.Blit(Texture2D.whiteTexture, temp, m_SceneResources.checkerboardMaterial);

            m_SceneResources.camera.targetTexture = temp;
            Graphics.DrawMesh(mesh, transform, m_PreviewMaterial, 1, m_SceneResources.camera, 0, m_PreviewPropertyBlock, ShadowCastingMode.Off, false, null, false);

            var previousUseSRP = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = renderData.shaderData.node is IMasterNode;
            m_SceneResources.camera.Render();
            Unsupported.useScriptableRenderPipeline = previousUseSRP;

            Graphics.Blit(temp, renderData.renderTexture, m_SceneResources.blitNoAlphaMaterial);
            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = previousRenderTexure;
            renderData.texture = renderData.renderTexture;
        }

        void UpdateShader(Identifier nodeId)
        {
            var node = m_Graph.GetNodeFromTempId(nodeId) as AbstractMaterialNode;
            if (node == null)
                return;
            var renderData = Get(m_RenderDatas, nodeId);
            if (renderData == null || renderData.shaderData == null)
                return;
            var shaderData = renderData.shaderData;

            if (!(node is IMasterNode) && !node.hasPreview)
            {
                shaderData.shaderString = null;
            }
            else
            {
                var masterNode = node as IMasterNode;
                if (masterNode != null)
                {
                    List<PropertyCollector.TextureInfo> configuredTextures;
                    shaderData.shaderString = masterNode.GetShader(GenerationMode.Preview, node.name, out configuredTextures);
                }
                else
                    shaderData.shaderString = m_Graph.GetPreviewShader(node).shader;
            }

            var debugOutputPath = DefaultShaderIncludes.GetDebugOutputPath();
            if (debugOutputPath != null)
                File.WriteAllText(debugOutputPath + "/GeneratedShader.shader", (shaderData.shaderString ?? "null").Replace("UnityEngine.MaterialGraph", "Generated"));

            if (string.IsNullOrEmpty(shaderData.shaderString))
            {
                if (shaderData.shader != null)
                {
                    ShaderUtil.ClearShaderErrors(shaderData.shader);
                    Object.DestroyImmediate(shaderData.shader, true);
                    shaderData.shader = null;
                }
                return;
            }

            if (shaderData.shader == null)
            {
                shaderData.shader = ShaderUtil.CreateShaderAsset(shaderData.shaderString);
                shaderData.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                ShaderUtil.ClearShaderErrors(shaderData.shader);
                ShaderUtil.UpdateShaderAsset(shaderData.shader, shaderData.shaderString);
            }

            // Debug output
            if (MaterialGraphAsset.ShaderHasError(shaderData.shader))
            {
                var errors = MaterialGraphAsset.GetShaderErrors(shaderData.shader);
                foreach (var error in errors)
                    Debug.LogFormat("Compilation error in {3} at line {1} (on {2}):\n{0}", error.message, error.line, error.platform, "graph");
                shaderData.hasError = true;
                if (debugOutputPath != null)
                {
                    var message = "RecreateShader: " + node.GetVariableNameForNode() + Environment.NewLine + shaderData.shaderString;
                    Debug.LogWarning(message);
                }
                ShaderUtil.ClearShaderErrors(shaderData.shader);
                Object.DestroyImmediate(shaderData.shader, true);
                shaderData.shader = null;
            }
            else
            {
                shaderData.hasError = false;
            }
        }

        void DestroyRenderData(PreviewRenderData renderData)
        {
            if (renderData.shaderData != null
                && renderData.shaderData.shader != null
                && renderData.shaderData.shader != m_ColorShader)
                Object.DestroyImmediate(renderData.shaderData.shader, true);
            if (renderData.renderTexture != null)
                Object.DestroyImmediate(renderData.renderTexture, true);

            if (renderData.shaderData != null && renderData.shaderData.node != null)
                renderData.shaderData.node.UnregisterCallback(OnNodeModified);
        }

        void DestroyPreview(Identifier nodeId)
        {
            var renderData = Get(m_RenderDatas, nodeId);
            if (renderData != null)
            {
                // Check if we're destroying the shader data used by the master preview
                if (masterRenderData != null && masterRenderData.shaderData != null && masterRenderData.shaderData == renderData.shaderData)
                    masterRenderData.shaderData = m_RenderDatas.Where(x => x != null && x.shaderData.node is IMasterNode && x != renderData).Select(x => x.shaderData).FirstOrDefault();

                DestroyRenderData(renderData);

                m_TimeDependentPreviews.Remove(nodeId.index);
                m_DirtyPreviews.Remove(nodeId.index);
                m_DirtyPreviews.Remove(nodeId.index);
                Set(m_RenderDatas, nodeId, null);
                Set(m_Identifiers, nodeId, default(Identifier));
            }
        }

        void ReleaseUnmanagedResources()
        {
            if (m_ColorShader != null)
            {
                Object.DestroyImmediate(m_ColorShader, true);
                m_ColorShader = null;
            }
            if (m_ErrorTexture != null)
            {
                Object.DestroyImmediate(m_ErrorTexture);
                m_ErrorTexture = null;
            }
            if (m_PreviewMaterial != null)
            {
                Object.DestroyImmediate(m_PreviewMaterial, true);
                m_PreviewMaterial = null;
            }
            if (m_SceneResources != null)
            {
                m_SceneResources.Dispose();
                m_SceneResources = null;
            }
            if (m_MasterRenderData != null)
                DestroyRenderData(m_MasterRenderData);
            foreach (var renderData in m_RenderDatas.ToList().Where(x => x != null))
                DestroyRenderData(renderData);
            m_RenderDatas.Clear();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PreviewManager()
        {
            throw new Exception("PreviewManager was not disposed of properly.");
        }

        const string k_EmptyShader = @"
Shader ""hidden/preview""
{
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma    vertex    vert
            #pragma    fragment    frag

            #include    ""UnityCG.cginc""

            struct    appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}";

        T Get<T>(List<T> list, Identifier id)
        {
            var existingId = Get(m_Identifiers, id.index);
            if (existingId.valid && existingId.version != id.version)
                throw new Exception("Identifier version mismatch");
            return Get(list, id.index);
        }

        static T Get<T>(List<T> list, int index)
        {
            return index < list.Count ? list[index] : default(T);
        }

        void Set<T>(List<T> list, Identifier id, T value)
        {
            var existingId = Get(m_Identifiers, id.index);
            if (existingId.valid && existingId.version != id.version)
                throw new Exception("Identifier version mismatch");
            Set(list, id.index, value);
        }

        static void Set<T>(List<T> list, int index, T value)
        {
            // Make sure the list is large enough for the index
            for (var i = list.Count; i <= index; i++)
                list.Add(default(T));
            list[index] = value;
        }

        PreviewRenderData GetRenderData(Identifier id)
        {
            var value = Get(m_RenderDatas, id);
            if (value != null && value.shaderData.node.tempId.version != id.version)
                throw new Exception("Trying to access render data of a previous version of a node");
            return value;
        }
    }

    public delegate void OnPreviewChanged();

    public class PreviewShaderData
    {
        public INode node { get; set; }
        public Shader shader { get; set; }
        public string shaderString { get; set; }
        public bool hasError { get; set; }
    }

    public class PreviewRenderData
    {
        public PreviewShaderData shaderData { get; set; }
        public RenderTexture renderTexture { get; set; }
        public Texture texture { get; set; }
        public PreviewMode previewMode { get; set; }
        public OnPreviewChanged onPreviewChanged;

        public void NotifyPreviewChanged()
        {
            if (onPreviewChanged != null)
                onPreviewChanged();
        }
    }
}
