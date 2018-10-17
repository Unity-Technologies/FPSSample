using System;

namespace UnityEditor.Graphing.Util
{
    public class TypeMapping
    {
        public Type fromType { get; private set; }
        public Type toType { get; private set; }

        public TypeMapping(Type fromType, Type toType)
        {
            this.fromType = fromType;
            this.toType = toType;
        }
    }
}
