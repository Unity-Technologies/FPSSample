namespace UnityEditor.ShaderGraph
{
    public interface IGenerateProperties
    {
        void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode);
    }
}
