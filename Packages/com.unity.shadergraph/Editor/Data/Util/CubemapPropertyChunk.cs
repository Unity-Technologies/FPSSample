using System;

namespace UnityEditor.ShaderGraph
{
/*    public class CubemapPropertyChunk : PropertyChunk
    {
        public enum ModifiableState
        {
            Modifiable,
            NonModifiable
        }

        private readonly Cubemap m_DefaultCube;
        private readonly ModifiableState m_Modifiable;

        public CubemapPropertyChunk(string propertyName, string propertyDescription, Cubemap defaultTexture, HideState hidden, ModifiableState modifiableState)
            : base(propertyName, propertyDescription, hidden)
        {
            m_DefaultCube = defaultTexture;
            m_Modifiable = modifiableState;
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            if (hideState == HideState.Hidden)
                result.Append("[HideInInspector] ");
            if (m_Modifiable == ModifiableState.NonModifiable)
                result.Append("[NonModifiableTextureData] ");

            result.Append(propertyName);
            result.Append("(\"");
            result.Append(propertyDescription);
            result.Append("\", Cube) = \"");
            result.Append("");
            result.Append("\" {}");
            return result.ToString();
        }

        public Texture defaultCube
        {
            get
            {
                return m_DefaultCube;
            }
        }
        public ModifiableState modifiableState
        {
            get
            {
                return m_Modifiable;
            }
        }
    }*/
}
