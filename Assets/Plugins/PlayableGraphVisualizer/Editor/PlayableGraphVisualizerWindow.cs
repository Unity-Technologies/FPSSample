using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;

namespace GraphVisualizer
{
    public class PlayableGraphVisualizerWindow : EditorWindow, IHasCustomMenu
    {
        private IGraphRenderer m_Renderer;
        private IGraphLayout m_Layout;

        private List<PlayableGraph> m_Graphs;
        private PlayableGraph m_CurrentGraph;
        private GraphSettings m_GraphSettings;

#region Configuration

        private static readonly float s_ToolbarHeight = 17f;
        private static readonly float s_DefaultMaximumNormalizedNodeSize = 0.8f;
        private static readonly float s_DefaultMaximumNodeSizeInPixels = 100.0f;
        private static readonly float s_DefaultAspectRatio = 1.5f;

#endregion

        private PlayableGraphVisualizerWindow()
        {
            m_GraphSettings.maximumNormalizedNodeSize = s_DefaultMaximumNormalizedNodeSize;
            m_GraphSettings.maximumNodeSizeInPixels = s_DefaultMaximumNodeSizeInPixels;
            m_GraphSettings.aspectRatio = s_DefaultAspectRatio;
            m_GraphSettings.showLegend = true;
        }

        [MenuItem("Window/PlayableGraph Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<PlayableGraphVisualizerWindow>("PlayableGraph Visualizer");
        }

        private PlayableGraph GetSelectedGraphInToolBar(List<PlayableGraph> graphs, PlayableGraph currentGraph)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));

            List<string> options = new List<string>(graphs.Count);
            foreach (var graph in graphs)
            {
                string name = graph.GetEditorName();
                options.Add(name.Length != 0 ? name : "[Unnamed]");
            }

            int currentSelection = graphs.IndexOf(currentGraph);
            int newSelection = EditorGUILayout.Popup(currentSelection != -1 ? currentSelection : 0, options.ToArray(), GUILayout.Width(200));

            PlayableGraph selectedGraph = new PlayableGraph();
            if (newSelection != -1)
                selectedGraph = graphs[newSelection];

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            return selectedGraph;
        }

        private static void ShowMessage(string msg)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(msg);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void Update()
        {
            // If in Play mode, refresh the graph each update.
            if (EditorApplication.isPlaying)
                Repaint();
        }

        void OnInspectorUpdate()
        {
            // If not in Play mode, refresh the graph less frequently.
            if (!EditorApplication.isPlaying)
                Repaint();
        }

        void OnEnable()
        {
            m_Graphs = new List<PlayableGraph>(UnityEditor.Playables.Utility.GetAllGraphs());

            UnityEditor.Playables.Utility.graphCreated += OnGraphCreated;
            UnityEditor.Playables.Utility.destroyingGraph += OnDestroyingGraph;
        }

        void OnGraphCreated(PlayableGraph graph)
        {
            if (!m_Graphs.Contains(graph))
                m_Graphs.Add(graph);
        }

        void OnDestroyingGraph(PlayableGraph graph)
        {
            m_Graphs.Remove(graph);
        }

        void OnDisable()
        {
            UnityEditor.Playables.Utility.graphCreated -= OnGraphCreated;
            UnityEditor.Playables.Utility.destroyingGraph -= OnDestroyingGraph;
        }

        void OnGUI()
        {
            // Early out if there is no graphs.
            var selectedGraphs = GetGraphList();
            if (selectedGraphs.Count == 0)
            {
                ShowMessage("No PlayableGraph in the scene");
                return;
            }

            GUILayout.BeginVertical();
            m_CurrentGraph = GetSelectedGraphInToolBar(selectedGraphs, m_CurrentGraph);
            GUILayout.EndVertical();

            if (!m_CurrentGraph.IsValid())
            {
                ShowMessage("Selected PlayableGraph is invalid");
                return;
            }

            var graph = new PlayableGraphVisualizer(m_CurrentGraph);
            graph.Refresh();

            if (graph.IsEmpty())
            {
                ShowMessage("Selected PlayableGraph is empty");
                return;
            }

            if (m_Layout == null)
                m_Layout = new ReingoldTilford();

            m_Layout.CalculateLayout(graph);

            var graphRect = new Rect(0, s_ToolbarHeight, position.width, position.height - s_ToolbarHeight);

            if (m_Renderer == null)
                m_Renderer = new DefaultGraphRenderer();

            m_Renderer.Draw(m_Layout, graphRect, m_GraphSettings);
        }

        private List<PlayableGraph> GetGraphList()
        {
            var selectedGraphs = new List<PlayableGraph>();
            foreach (var clientGraph in GraphVisualizerClient.GetGraphs())
            {
                if (clientGraph.IsValid())
                    selectedGraphs.Add(clientGraph);
            }

            if (selectedGraphs.Count == 0)
                selectedGraphs = m_Graphs.ToList();

            return selectedGraphs;
        }

#region Custom_Menu

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Legend"), m_GraphSettings.showLegend, ToggleLegend);
        }

        void ToggleLegend()
        {
            m_GraphSettings.showLegend = !m_GraphSettings.showLegend;
        }

#endregion
    }
}
