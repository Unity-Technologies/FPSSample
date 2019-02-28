using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class AttributeProvider : IStringProvider
    {
        public string[] GetAvailableString()
        {
            return VFXAttribute.AllIncludingVariadicExceptLocalOnly.ToArray();
        }
    }

    class ReadWritableAttributeProvider : IStringProvider
    {
        public string[] GetAvailableString()
        {
            return VFXAttribute.AllIncludingVariadicReadWritable.ToArray();
        }
    }

    class AttributeVariant : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllIncludingVariadicExceptLocalOnly.Cast<object>().ToArray() }
                };
            }
        }
    }

    class AttributeVariantReadWritable : VariantProvider
    {
        protected override Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllIncludingVariadicReadWritable.Cast<object>().ToArray() }
                };
            }
        }
    }

    class AttributeVariantReadWritableNoVariadic : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "attribute", VFXAttribute.AllReadWritable.Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeVariant))]
    class VFXAttributeParameter : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.AllIncludingVariadic.First();

        [VFXSetting, Tooltip("Select the version of this parameter that is used.")]
        public VFXAttributeLocation location = VFXAttributeLocation.Current;

        [VFXSetting, Regex("[^x-zX-Z]", 3)]
        public string mask = "xyz";

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (string setting in base.filteredOutSettings) yield return setting;
                var attribute = VFXAttribute.Find(this.attribute);
                if (attribute.variadic == VFXVariadic.False) yield return "mask";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var attribute = VFXAttribute.Find(this.attribute);
                if (attribute.variadic == VFXVariadic.True)
                {
                    Type slotType = null;
                    switch (mask.Length)
                    {
                        case 1: slotType = typeof(float); break;
                        case 2: slotType = typeof(Vector2); break;
                        case 3: slotType = typeof(Vector3); break;
                        case 4: slotType = typeof(Vector4); break;
                        default: break;
                    }

                    if (slotType != null)
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, attribute.name));
                }
                else
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(attribute.type), attribute.name));
                }
            }
        }

        override public string libraryName { get { return "Get Attribute: " + attribute; } }
        override public string name
        {
            get
            {
                string result = string.Format("Get Attribute: {0} ({1})", attribute, location);

                try
                {
                    var attrib = VFXAttribute.Find(this.attribute);
                    if (attrib.variadic == VFXVariadic.True)
                        result += "." + mask;
                }
                catch {} // Must not throw in name getter

                return result;
            }
        }

        public override void Sanitize(int version)
        {
            if (!VFXAttribute.Exist(attribute))
            {
                Debug.LogWarningFormat("Attribute parameter was removed because attribute {0} does not exist", attribute);
                RemoveModel(this, false);
                return; // Dont sanitize further, model was removed
            }

            UnityEditor.VFX.Block.VFXBlockUtility.SanitizeAttribute(ref attribute, ref mask, version);
            base.Sanitize(version);
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var attribute = VFXAttribute.Find(this.attribute);
            if (attribute.variadic == VFXVariadic.True)
            {
                var attributes = new VFXAttribute[] { VFXAttribute.Find(attribute.name + "X"), VFXAttribute.Find(attribute.name + "Y"), VFXAttribute.Find(attribute.name + "Z") };
                var expressions = attributes.Select(a => new VFXAttributeExpression(a, location)).ToArray();

                var componentStack = new Stack<VFXExpression>();
                int outputSize = mask.Length;
                for (int iComponent = 0; iComponent < outputSize; iComponent++)
                {
                    char componentChar = char.ToLower(mask[iComponent]);
                    int currentComponent = Math.Min(componentChar - 'x', 2);
                    componentStack.Push(expressions[currentComponent]);
                }

                VFXExpression finalExpression = null;
                if (componentStack.Count == 1)
                {
                    finalExpression = componentStack.Pop();
                }
                else
                {
                    finalExpression = new VFXExpressionCombine(componentStack.Reverse().ToArray());
                }
                return new[] { finalExpression };
            }
            else
            {
                var expression = new VFXAttributeExpression(attribute, location);
                return new VFXExpression[] { expression };
            }
        }
    }
}
