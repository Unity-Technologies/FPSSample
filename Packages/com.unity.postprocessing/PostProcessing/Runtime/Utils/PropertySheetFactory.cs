using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    public sealed class PropertySheetFactory
    {
        readonly Dictionary<Shader, PropertySheet> m_Sheets;

        public PropertySheetFactory()
        {
            m_Sheets = new Dictionary<Shader, PropertySheet>();
        }

        public PropertySheet Get(string shaderName)
        {
            var shader = Shader.Find(shaderName);

            if (shader == null)
                throw new ArgumentException(string.Format("Invalid shader ({0})", shaderName));

            return Get(shader);
        }

        public PropertySheet Get(Shader shader)
        {
            PropertySheet sheet;

            if (shader == null)
                throw new ArgumentException(string.Format("Invalid shader ({0})", shader));

            if (m_Sheets.TryGetValue(shader, out sheet))
                return sheet;

            var shaderName = shader.name;
            var material = new Material(shader)
            {
                name = string.Format("PostProcess - {0}", shaderName.Substring(shaderName.LastIndexOf('/') + 1)),
                hideFlags = HideFlags.DontSave
            };

            sheet = new PropertySheet(material);
            m_Sheets.Add(shader, sheet);
            return sheet;
        }

        public void Release()
        {
            foreach (var sheet in m_Sheets.Values)
                sheet.Release();

            m_Sheets.Clear();
        }
    }
}
