namespace UnityEditor.ShaderGraph
{
    public enum GenerationMode
    {
        Preview,
        ForReals
    }
    public static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview; }
    }
}
