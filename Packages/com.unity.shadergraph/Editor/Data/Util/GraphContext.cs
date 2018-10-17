using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class GraphContext
    {
        public GraphContext(string inputStructName)
        {
            graphInputStructName = inputStructName;
        }

        public string graphInputStructName
        {
            get { return m_GraphInputStructName; }
            set { m_GraphInputStructName = value; }
        }

        string m_GraphInputStructName;
    }
}
