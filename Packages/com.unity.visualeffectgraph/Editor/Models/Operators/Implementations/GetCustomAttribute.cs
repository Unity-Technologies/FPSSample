using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Attribute", experimental = true)]
    class GetCustomAttribute : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Delayed]
        public string attribute = "CustomAttribute";

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public CustomAttributeUtility.Signature AttributeType = CustomAttributeUtility.Signature.Float;

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var attribute = currentAttribute;
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(attribute.type), attribute.name));
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return new VFXAttribute(attribute, CustomAttributeUtility.GetValueType(AttributeType));
            }
        }

        override public string libraryName { get { return "Get Custom Attribute"; } }

        override public string name
        {
            get
            {
                return "Get " + attribute + " ("+AttributeType.ToString()+")";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var attribute = currentAttribute;
 
            var expression = new VFXAttributeExpression(attribute, VFXAttributeLocation.Current);
            return new VFXExpression[] { expression };
        }
    }
}
