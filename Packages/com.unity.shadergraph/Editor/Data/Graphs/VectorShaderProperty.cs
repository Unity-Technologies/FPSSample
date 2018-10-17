using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Vector) = (");
            result.Append(NodeUtils.FloatToShaderValue(value.x));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.y));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.z));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.w));
            result.Append(")");
            return result.ToString();
        }
    }
}
