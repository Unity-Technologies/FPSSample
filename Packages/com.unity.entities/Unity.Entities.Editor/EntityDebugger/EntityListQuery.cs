
using System.Linq;
using Unity.Entities;

namespace Unity.Entities.Editor
{
    
    public class EntityListQuery
    {

        public ComponentGroup Group { get; }

        public EntityArchetypeQuery Query { get; }

        public EntityListQuery(ComponentGroup group)
        {
            this.Group = group;
            this.Query = new EntityArchetypeQuery()
            {
                All = group.Types.Where(x => x.AccessModeType != ComponentType.AccessMode.Subtractive).ToArray(),
                None = group.Types.Where(x => x.AccessModeType == ComponentType.AccessMode.Subtractive).ToArray(),
                Any = new ComponentType[0]
            };
        }

        public EntityListQuery(EntityArchetypeQuery query)
        {
            this.Query = query;
        }
    }

}

