using System;

namespace UnityEditor.ShaderGraph
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class TitleAttribute : Attribute
    {
        public string[] title;
        public TitleAttribute(params string[] title) { this.title = title; }
    }
}
