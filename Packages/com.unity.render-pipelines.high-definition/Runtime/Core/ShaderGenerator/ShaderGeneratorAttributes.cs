using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public enum PackingRules
    {
        Exact,
        Aggressive
    };

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
    public class GenerateHLSL : System.Attribute
    {
        public PackingRules packingRules;
        public bool needAccessors; // Whether or not to generate the accessors
        public bool needParamDebug; // // Whether or not to generate define for each field of the struct + debug function (use in HDRenderPipeline)
        public int paramDefinesStart; // Start of the generated define
        public bool omitStructDeclaration; // Whether to skip "struct <name> {" etc

        public GenerateHLSL(PackingRules rules = PackingRules.Exact, bool needAccessors = true, bool needParamDebug = false, int paramDefinesStart = 1, bool omitStructDeclaration = false)
        {
            packingRules = rules;
            this.needAccessors = needAccessors;
            this.needParamDebug = needParamDebug;
            this.paramDefinesStart = paramDefinesStart;
            this.omitStructDeclaration = omitStructDeclaration;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SurfaceDataAttributes : System.Attribute
    {
        public string[] displayNames;
        public bool isDirection;
        public bool sRGBDisplay;

        public SurfaceDataAttributes(string displayName = "", bool isDirection = false, bool sRGBDisplay = false)
        {
            displayNames = new string[1];
            displayNames[0] = displayName;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
        }

        // We allow users to add several names for one field, so user can override the auto behavior and do something else with the same data
        // typical example is normal that you want to draw in view space or world space. So user can override view space case and do the transform.
        public SurfaceDataAttributes(string[] displayName, bool isDirection = false, bool sRGBDisplay = false)
        {
            displayNames = displayName;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class HLSLArray : System.Attribute
    {
        public int  arraySize;
        public Type elementType;

        public HLSLArray(int arraySize, Type elementType)
        {
            this.arraySize = arraySize;
            this.elementType = elementType;
        }
    }
}
