namespace UnityEditor.ShaderGraph
{
    public interface IGeneratesFunction
    {
        void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode);
    }
}
