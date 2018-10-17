namespace UnityEditor.ShaderGraph
{
    public interface IMaterialSlotHasValue<T>
    {
        T defaultValue { get; }
        T value { get; }
    }
}
