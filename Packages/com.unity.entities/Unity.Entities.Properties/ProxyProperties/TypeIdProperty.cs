using Unity.Properties;

namespace Unity.Entities.Properties
{
    internal class TypeIdStructProperty : ValueStructProperty<StructProxy, string>
    {
        public TypeIdStructProperty(GetValueMethod getValue) : base("$TypeId", getValue, null)
        {
        }
    }

    internal class TypeIdClassProperty : ValueClassProperty<ObjectContainerProxy, string>
    {
        public TypeIdClassProperty(GetValueMethod getValue) : base("$TypeId", getValue, null)
        {
        }
    }
}
