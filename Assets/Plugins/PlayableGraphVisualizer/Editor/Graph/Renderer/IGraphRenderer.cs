using UnityEngine;

namespace GraphVisualizer
{
    // Interface for rendering a tree layout to screen.
    public interface IGraphRenderer
    {
        void Draw(IGraphLayout graphLayout, Rect drawingArea);
        void Draw(IGraphLayout graphLayout, Rect drawingArea, GraphSettings graphSettings);
    }

    // Customization of how the graph will be displayed:
    // - size, distances and proportions of nodes
    // - legend
    public struct GraphSettings
    {
        // In layout units. If 1, node will be drawn as large as possible to avoid overlapping, and to respect aspect ratio
        public float maximumNormalizedNodeSize;

        // when the graph is very simple, the nodes can seem disproportionate relative to the graph. This limits their size
        public float maximumNodeSizeInPixels;

        // width / height; 1 represents a square node
        public float aspectRatio;

        // Control the display of the legend.
        public bool showLegend;
    }
}