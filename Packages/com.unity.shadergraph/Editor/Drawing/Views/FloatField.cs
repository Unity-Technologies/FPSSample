using UnityEditor.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class FloatField : DoubleField
    {
        protected override string ValueToString(double v)
        {
            return ((float)v).ToString();
        }
    }
}
