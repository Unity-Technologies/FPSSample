namespace UnityEditor.Graphing
{
    public interface IGraphObject
    {
        IGraph graph { get; set; }
        void RegisterCompleteObjectUndo(string name);
    }
}
