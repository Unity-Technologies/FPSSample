namespace UnityEditor.ShaderGraph
{
    public interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode);
    }
}
