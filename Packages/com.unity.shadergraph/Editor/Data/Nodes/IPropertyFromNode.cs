namespace UnityEditor.ShaderGraph
{
    interface IPropertyFromNode
    {
        IShaderProperty AsShaderProperty();
        int outputSlotId { get; }
    }
}
