using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace GraphVisualizer
{
    public class DefaultGraphRenderer : IGraphRenderer
    {
        private static readonly Color s_EdgeColorMin = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        private static readonly Color s_EdgeColorMax = Color.white;
        private static readonly Color s_LegendBackground = new Color(0, 0, 0, 0.1f);

        private static readonly float s_BorderSize = 15;
        private static readonly float s_LegendFixedOverheadWidth = 100;
        private static readonly float s_DefaultMaximumNormalizedNodeSize = 0.8f;
        private static readonly float s_DefaultMaximumNodeSizeInPixels = 100.0f;
        private static readonly float s_DefaultAspectRatio = 1.5f;

        private static readonly int s_NodeMaxFontSize = 14;

        private GUIStyle m_LegendLabelStyle;
        private GUIStyle m_SubTitleStyle;
        private GUIStyle m_InspectorStyle;
        private GUIStyle m_NodeRectStyle;

        private static readonly int s_ActiveNodeThickness = 2;
        private static readonly int s_SelectedNodeThickness = 4;
        private static readonly Color s_ActiveNodeColor = Color.white;
        private static readonly Color s_SelectedNodeColor = Color.yellow;

        private readonly Dictionary<string, NodeTypeLegend> m_LegendForType = new Dictionary<string, NodeTypeLegend>();

        private Node m_SelectedNode;

        private Texture2D m_ColorBar;

        private struct NodeTypeLegend
        {
            public Color color;
            public string label;
        }

        public DefaultGraphRenderer()
        {
            InitializeStyles();
        }

        public void Draw(IGraphLayout graphLayout, Rect drawingArea)
        {
            GraphSettings defaults;
            defaults.maximumNormalizedNodeSize = s_DefaultMaximumNormalizedNodeSize;
            defaults.maximumNodeSizeInPixels = s_DefaultMaximumNodeSizeInPixels;
            defaults.aspectRatio = s_DefaultAspectRatio;
            defaults.showLegend = true;
            Draw(graphLayout, drawingArea, defaults);
        }

        public void Draw(IGraphLayout graphLayout, Rect totalDrawingArea, GraphSettings graphSettings)
        {
            var legendArea = new Rect();
            var drawingArea = new Rect(totalDrawingArea);

            if (graphSettings.showLegend)
            {
                PrepareLegend(graphLayout.vertices);

                legendArea = new Rect(totalDrawingArea)
                {
                    width = EstimateLegendWidth() + s_BorderSize * 2
                };

                legendArea.x = drawingArea.xMax - legendArea.width;
                drawingArea.width -= legendArea.width;// + s_BorderSize;

                DrawLegend(legendArea);
            }

            if (m_SelectedNode != null)
            {
                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                {
                    Vector2 mousePos = currentEvent.mousePosition;
                    if (drawingArea.Contains(mousePos))
                    {
                        m_SelectedNode = null;
                    }
                }
            }

            DrawGraph(graphLayout, drawingArea, graphSettings);
        }

        private void InitializeStyles()
        {
            m_LegendLabelStyle = new GUIStyle(GUI.skin.label)
            {
                margin = {top = 0},
                       alignment = TextAnchor.UpperLeft
            };

            m_NodeRectStyle = new GUIStyle
            {
                normal =
                {
                    background = (Texture2D) Resources.Load("Node"),
                    textColor = Color.black,
                },
                    border = new RectOffset(10, 10, 10, 10),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    clipping = TextClipping.Clip

            };

            m_SubTitleStyle = EditorStyles.boldLabel;

            m_InspectorStyle = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white,
                },
                    richText = true,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    clipping = TextClipping.Clip

            };
        }

        private void PrepareLegend(IEnumerable<Vertex> vertices)
        {
            m_LegendForType.Clear();
            foreach (Vertex v in vertices)
            {
                if (v.node == null)
                    continue;

                string nodeType = v.node.GetContentTypeName();

                if (m_LegendForType.ContainsKey(nodeType))
                    continue;

                m_LegendForType[nodeType] = new NodeTypeLegend
                {
                    label = v.node.GetContentTypeShortName(),
                          color = v.node.GetColor()
                };
            }
        }

        private float EstimateLegendWidth()
        {
            float legendWidth = 0;
            foreach (NodeTypeLegend legend in m_LegendForType.Values)
            {
                legendWidth = Mathf.Max(legendWidth, GUI.skin.label.CalcSize(new GUIContent(legend.label)).x);
            }
            legendWidth += s_LegendFixedOverheadWidth;
            return legendWidth;
        }

        public void DrawRect(Rect rect, Color color, string text, bool active, bool selected = false)
        {
            var originalColor = GUI.color;

            if (selected)
            {
                GUI.color = s_SelectedNodeColor;
                float t = s_SelectedNodeThickness + (active ? s_ActiveNodeThickness : 0.0f);
                GUI.Box(new Rect(rect.x - t, rect.y - t,
                            rect.width + 2 * t, rect.height + 2 * t),
                        string.Empty, m_NodeRectStyle);
            }

            if (active)
            {
                GUI.color = s_ActiveNodeColor;
                GUI.Box(new Rect(rect.x - s_ActiveNodeThickness, rect.y - s_ActiveNodeThickness,
                            rect.width + 2 * s_ActiveNodeThickness, rect.height + 2 * s_ActiveNodeThickness),
                        string.Empty, m_NodeRectStyle);
            }

            // Body + Text
            GUI.color = color;
            m_NodeRectStyle.fontSize = ComputeFontSize(rect.size, text);
            GUI.Box(rect, text, m_NodeRectStyle);

            GUI.color = originalColor;
        }

        private void DrawLegend(Rect legendArea)
        {
            EditorGUI.DrawRect(legendArea, s_LegendBackground);

            // Add a border around legend area
            legendArea.x += s_BorderSize;
            legendArea.width -= s_BorderSize * 2;
            legendArea.y += s_BorderSize;
            legendArea.height -= s_BorderSize * 2;

            GUILayout.BeginArea(legendArea);
            GUILayout.BeginVertical();

            GUILayout.Label("Inspector", m_SubTitleStyle);

            if (m_SelectedNode != null)
            {
                GUILayout.Label(m_SelectedNode.ToString(), m_InspectorStyle);
            }
            else
            {
                GUILayout.Label("Click on a node\nto display its details.");
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Legend", m_SubTitleStyle);

            foreach (var pair in m_LegendForType)
            {
                DrawLegendEntry(pair.Value.color, pair.Value.label, false);
            }

            DrawLegendEntry(Color.gray, "Playing", true);

            GUILayout.Space(20);

            GUILayout.Label("Edge weight", m_SubTitleStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("0");
            GUILayout.FlexibleSpace();
            GUILayout.Label("1");
            GUILayout.EndHorizontal();

            DrawEdgeWeightColorBar(legendArea.width);

            GUILayout.Space(20);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawLegendEntry(Color color, string label, bool active)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Height(20));

            Rect legendIconRect = GUILayoutUtility.GetRect(1, 1, GUILayout.Width(20), GUILayout.Height(20));
            DrawRect(legendIconRect, color, string.Empty, active);

            GUILayout.Label(label, m_LegendLabelStyle);

            GUILayout.EndHorizontal();
        }

        private void DrawEdgeWeightColorBar(float width)
        {
            const int nbLevels = 64;

            if (m_ColorBar == null)
            {
                m_ColorBar = new Texture2D(nbLevels, 1)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                var cols = m_ColorBar.GetPixels();
                for (int x = 0; x < nbLevels; x++)
                {
                    Color c = Color.Lerp(s_EdgeColorMin, s_EdgeColorMax, (float) x / nbLevels);
                    cols[x] = c;
                }

                m_ColorBar.SetPixels(cols);
                m_ColorBar.Apply(false);
            }

            const int colorbarHeight = 20;
            GUI.DrawTexture(GUILayoutUtility.GetRect(width, colorbarHeight), m_ColorBar);
        }

        // Draw the graph and returns the selected Node if there's any.
        private void DrawGraph(IGraphLayout graphLayout, Rect drawingArea, GraphSettings graphSettings)
        {
            // add border, except on right-hand side where the legend will provide necessary padding
            drawingArea = new Rect(drawingArea.x + s_BorderSize,
                    drawingArea.y + s_BorderSize,
                    drawingArea.width - s_BorderSize * 2,
                    drawingArea.height - s_BorderSize * 2);

            var b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Vertex v in graphLayout.vertices)
            {
                b.Encapsulate(new Vector3(v.position.x, v.position.y, 0.0f));
            }

            // Increase b by maximum node size (since b is measured between node centers)
            b.Expand(new Vector3(graphSettings.maximumNormalizedNodeSize, graphSettings.maximumNormalizedNodeSize, 0));

            var scale = new Vector2(drawingArea.width / b.size.x, drawingArea.height / b.size.y);
            var offset = new Vector2(-b.min.x, -b.min.y);

            Vector2 nodeSize = ComputeNodeSize(scale, graphSettings);

            GUI.BeginGroup(drawingArea);

            foreach (var e in graphLayout.edges)
            {
                Vector2 v0 = ScaleVertex(e.source.position, offset, scale);
                Vector2 v1 = ScaleVertex(e.destination.position, offset, scale);
                Node node = e.source.node;

                if (graphLayout.leftToRight)
                    DrawEdge(v1, v0, node.weight);
                else
                    DrawEdge(v0, v1, node.weight);
            }

            Event currentEvent = Event.current;

            bool oldSelectionFound = false;
            Node newSelectedNode = null;

            foreach (Vertex v in graphLayout.vertices)
            {
                Vector2 nodeCenter = ScaleVertex(v.position, offset, scale) - nodeSize / 2;
                var nodeRect = new Rect(nodeCenter.x, nodeCenter.y, nodeSize.x, nodeSize.y);

                bool clicked = false;
                if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                {
                    Vector2 mousePos = currentEvent.mousePosition;
                    if (nodeRect.Contains(mousePos))
                    {
                        clicked = true;
                        currentEvent.Use();
                    }
                }

                bool currentSelection = (m_SelectedNode != null)
                    && v.node.content.Equals(m_SelectedNode.content); // Make sure to use Equals() and not == to call any overriden comparison operator in the content type.

                DrawNode(nodeRect, v.node, currentSelection || clicked);

                if (currentSelection)
                {
                    // Previous selection still there.
                    oldSelectionFound = true;
                }
                else if (clicked)
                {
                    // Just Selected a new node.
                    newSelectedNode = v.node;
                }
            }

            if (newSelectedNode != null)
            {
                m_SelectedNode = newSelectedNode;
            }
            else if (!oldSelectionFound)
            {
                m_SelectedNode = null;
            }

            GUI.EndGroup();
        }

        // Apply node constraints to node size
        private static Vector2 ComputeNodeSize(Vector2 scale, GraphSettings graphSettings)
        {
            var extraTickness = (s_SelectedNodeThickness + s_ActiveNodeThickness) * 2.0f;
            var nodeSize = new Vector2(graphSettings.maximumNormalizedNodeSize * scale.x - extraTickness,
                    graphSettings.maximumNormalizedNodeSize * scale.y - extraTickness);

            // Adjust aspect ratio after scaling
            float currentAspectRatio = nodeSize.x / nodeSize.y;

            if (currentAspectRatio > graphSettings.aspectRatio)
            {
                // Shrink x dimension
                nodeSize.x = nodeSize.y * graphSettings.aspectRatio;
            }
            else
            {
                // Shrink y dimension
                nodeSize.y = nodeSize.x / graphSettings.aspectRatio;
            }

            // If node size is still too big, scale down
            if (nodeSize.x > graphSettings.maximumNodeSizeInPixels)
            {
                nodeSize *= graphSettings.maximumNodeSizeInPixels / nodeSize.x;
            }

            if (nodeSize.y > graphSettings.maximumNodeSizeInPixels)
            {
                nodeSize *= graphSettings.maximumNodeSizeInPixels / nodeSize.y;
            }
            return nodeSize;
        }

        private static int ComputeFontSize(Vector2 nodeSize, string text)
        {
            if (string.IsNullOrEmpty(text))
                return s_NodeMaxFontSize;

            string[] words = text.Split('\n');
            int nbLignes = words.Length;
            int longuestWord = words.Max(s => s.Length);

            // Approximate the text rectangle size using magic values.
            int width = longuestWord * (int) (0.8f * s_NodeMaxFontSize);
            int height = nbLignes * (int) (1.5f * s_NodeMaxFontSize);

            float factor = Math.Min(nodeSize.x / width, nodeSize.y / height);

            factor = Mathf.Clamp01(factor);

            return Mathf.CeilToInt(s_NodeMaxFontSize * factor);
        }

        // Convert vertex position from normalized layout to render rect
        private static Vector2 ScaleVertex(Vector2 v, Vector2 offset, Vector2 scaleFactor)
        {
            return new Vector2((v.x + offset.x) * scaleFactor.x, (v.y + offset.y) * scaleFactor.y);
        }

        // Draw a node an return true if it has been clicked
        private void DrawNode(Rect nodeRect, Node node, bool selected)
        {
            string nodeType = node.GetContentTypeName();
            NodeTypeLegend nodeTypeLegend = m_LegendForType[nodeType];
            string formatedLabel = Regex.Replace(nodeTypeLegend.label, "(\\B[A-Z])", "\n$1"); // Split into multi-lines

            DrawRect(nodeRect, nodeTypeLegend.color, formatedLabel, node.active, selected);
        }

        // Compute the tangents for the graphLayout edges. Assumes that graphLayout is drawn from left to right
        private static void GetTangents(Vector2 start, Vector2 end, out Vector3[] points, out Vector3[] tangents)
        {
            points = new Vector3[] {start, end};
            tangents = new Vector3[2];

            // Heuristics to define the length of the tangents and tweak the look of the bezier curves.
            const float minTangent = 30;
            const float weight = 0.5f;
            float cleverness = Mathf.Clamp01(((start - end).magnitude - 10) / 50);
            tangents[0] = start + new Vector2((end.x - start.x) * weight + minTangent, 0) * cleverness;
            tangents[1] = end + new Vector2((end.x - start.x) * -weight - minTangent, 0) * cleverness;
        }

        private static void DrawEdge(Vector2 a, Vector2 b, float weight)
        {
            Vector3[] points, tangents;

            GetTangents(a, b, out points, out tangents);

            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1],
                    Color.Lerp(s_EdgeColorMin, s_EdgeColorMax, weight), null, 5f);
        }
    }
}
