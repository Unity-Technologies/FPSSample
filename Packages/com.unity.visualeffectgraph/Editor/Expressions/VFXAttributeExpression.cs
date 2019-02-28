using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Flags]
    enum VFXAttributeMode
    {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write,
        ReadSource = 1 << 2,
    }

    struct VFXAttribute
    {
        public static readonly float kDefaultSize = 0.1f;

        public static readonly VFXAttribute Seed                = new VFXAttribute("seed", VFXValueType.Uint32);
        public static readonly VFXAttribute OldPosition         = new VFXAttribute("oldPosition", VFXValueType.Float3);
        public static readonly VFXAttribute Position            = new VFXAttribute("position", VFXValueType.Float3);
        public static readonly VFXAttribute Velocity            = new VFXAttribute("velocity", VFXValueType.Float3);
        public static readonly VFXAttribute Direction           = new VFXAttribute("direction", VFXValue.Constant(new Vector3(0.0f, 0.0f, 1.0f)));
        public static readonly VFXAttribute Color               = new VFXAttribute("color", VFXValue.Constant(Vector3.one));
        public static readonly VFXAttribute Alpha               = new VFXAttribute("alpha", VFXValue.Constant(1.0f));
        public static readonly VFXAttribute Size                = new VFXAttribute("size", VFXValue.Constant(kDefaultSize));
        public static readonly VFXAttribute ScaleX              = new VFXAttribute("scaleX", VFXValue.Constant(1.0f), VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute ScaleY              = new VFXAttribute("scaleY", VFXValue.Constant(1.0f), VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute ScaleZ              = new VFXAttribute("scaleZ", VFXValue.Constant(1.0f), VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute Lifetime            = new VFXAttribute("lifetime", VFXValueType.Float);
        public static readonly VFXAttribute Age                 = new VFXAttribute("age", VFXValueType.Float);
        public static readonly VFXAttribute AngleX              = new VFXAttribute("angleX", VFXValueType.Float, VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngleY              = new VFXAttribute("angleY", VFXValueType.Float, VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngleZ              = new VFXAttribute("angleZ", VFXValueType.Float, VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngularVelocityX    = new VFXAttribute("angularVelocityX", VFXValueType.Float, VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngularVelocityY    = new VFXAttribute("angularVelocityY", VFXValueType.Float, VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute AngularVelocityZ    = new VFXAttribute("angularVelocityZ", VFXValueType.Float, VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute TexIndex            = new VFXAttribute("texIndex", VFXValueType.Float);
        public static readonly VFXAttribute PivotX              = new VFXAttribute("pivotX", VFXValue.Constant(0.0f), VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute PivotY              = new VFXAttribute("pivotY", VFXValue.Constant(0.0f), VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute PivotZ              = new VFXAttribute("pivotZ", VFXValue.Constant(0.0f), VFXVariadic.BelongsToVariadic);
        public static readonly VFXAttribute ParticleId          = new VFXAttribute("particleId", VFXValueType.Uint32);
        public static readonly VFXAttribute AxisX               = new VFXAttribute("axisX", VFXValue.Constant(Vector3.right));
        public static readonly VFXAttribute AxisY               = new VFXAttribute("axisY", VFXValue.Constant(Vector3.up));
        public static readonly VFXAttribute AxisZ               = new VFXAttribute("axisZ", VFXValue.Constant(Vector3.forward));
        public static readonly VFXAttribute Alive               = new VFXAttribute("alive", VFXValue.Constant(true));
        public static readonly VFXAttribute Mass                = new VFXAttribute("mass", VFXValue.Constant(1.0f));
        public static readonly VFXAttribute TargetPosition      = new VFXAttribute("targetPosition", VFXValueType.Float3);
        public static readonly VFXAttribute EventCount          = new VFXAttribute("eventCount", VFXValueType.Uint32);
        public static readonly VFXAttribute SpawnTime           = new VFXAttribute("spawnTime", VFXValueType.Float);


        public static readonly VFXAttribute[] AllAttribute = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXAttribute>(typeof(VFXAttribute));
        public static readonly VFXAttribute[] AllAttributeReadOnly = new VFXAttribute[] { Seed, ParticleId, SpawnTime };
        public static readonly VFXAttribute[] AllAttributeWriteOnly = new VFXAttribute[] { EventCount };
        public static readonly VFXAttribute[] AllAttributeLocalOnly = new VFXAttribute[] { EventCount };

        public static readonly string[] All = AllAttribute.Select(e => e.name).ToArray();
        public static readonly string[] AllReadOnly = AllAttributeReadOnly.Select(e => e.name).ToArray();
        public static readonly string[] AllLocalOnly = AllAttributeLocalOnly.Select(e => e.name).ToArray();
        public static readonly string[] AllWriteOnly = AllAttributeWriteOnly.Select(e => e.name).ToArray();

        public static readonly string[] AllExceptLocalOnly = All.Except(AllLocalOnly).ToArray();
        public static readonly string[] AllWritable = All.Except(AllReadOnly).ToArray();
        public static readonly string[] AllReadWritable = All.Except(AllReadOnly).Except(AllWriteOnly).ToArray();

        public static readonly VFXAttribute[] AllVariadicAttribute = new VFXAttribute[]
        {
            new VFXAttribute("angle", VFXValueType.Float3, VFXVariadic.True),
            new VFXAttribute("angularVelocity", VFXValueType.Float3, VFXVariadic.True),
            new VFXAttribute("pivot", VFXValueType.Float3, VFXVariadic.True),
            new VFXAttribute("scale", VFXValueType.Float3, VFXVariadic.True)
        };

        public static readonly string[] AllVariadic = AllVariadicAttribute.Select(e => e.name).ToArray();

        public static readonly string[] AllIncludingVariadic = AllAttribute.Where(e => e.variadic != VFXVariadic.BelongsToVariadic).Select(e => e.name).ToArray().Concat(AllVariadic).ToArray();
        public static readonly string[] AllIncludingVariadicExceptLocalOnly = AllIncludingVariadic.Except(AllLocalOnly).ToArray();
        public static readonly string[] AllIncludingVariadicWritable = AllIncludingVariadic.Except(AllReadOnly).ToArray();
        public static readonly string[] AllIncludingVariadicReadWritable = AllIncludingVariadic.Except(AllReadOnly).Except(AllWriteOnly).ToArray();

        static private VFXValue GetValueFromType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Boolean: return VFXValue.Constant<bool>();
                case VFXValueType.Uint32: return VFXValue.Constant<uint>();
                case VFXValueType.Int32: return VFXValue.Constant<int>();
                case VFXValueType.Float: return VFXValue.Constant<float>();
                case VFXValueType.Float2: return VFXValue.Constant<Vector2>();
                case VFXValueType.Float3: return VFXValue.Constant<Vector3>();
                case VFXValueType.Float4: return VFXValue.Constant<Vector4>();
                default: throw new InvalidOperationException(string.Format("Unexpected attribute type: {0}", type));
            }
        }

        public VFXAttribute(string name, VFXValueType type, VFXVariadic variadic = VFXVariadic.False)
        {
            this.name = name;
            this.value = GetValueFromType(type);
            this.variadic = variadic;
        }

        public VFXAttribute(string name, VFXValue value, VFXVariadic variadic = VFXVariadic.False)
        {
            this.name = name;
            this.value = value;
            this.variadic = variadic;
        }

        public static VFXAttribute Find(string attributeName)
        {
            int index = Array.FindIndex(AllAttribute, e => e.name == attributeName);
            if (index != -1)
                return AllAttribute[index];

            index = Array.FindIndex(AllVariadicAttribute, e => e.name == attributeName);
            if (index != -1)
                return AllVariadicAttribute[index];

            throw new ArgumentException(string.Format("Unable to find attribute expression : {0}", attributeName));
        }

        public static bool Exist(string attributeName)
        {
            bool exist = Array.Exists(AllAttribute, e => e.name == attributeName);

            if (!exist)
                exist = Array.Exists(AllVariadicAttribute, e => e.name == attributeName);

            return exist;
        }

        public string name;
        public VFXValue value;
        public VFXVariadic variadic;

        public VFXValueType type
        {
            get
            {
                return value.valueType;
            }
        }
    }

    struct VFXAttributeInfo
    {
        public VFXAttributeInfo(VFXAttribute attrib, VFXAttributeMode mode)
        {
            this.attrib = attrib;
            this.mode = mode;
        }

        public VFXAttribute attrib;
        public VFXAttributeMode mode;
    }

    enum VFXAttributeLocation
    {
        Current = 0,
        Source = 1,
    }

    enum VFXVariadic
    {
        False = 0,
        True = 1,
        BelongsToVariadic = 2
    }

    enum VariadicChannelOptions
    {
        X = 0,
        Y = 1,
        Z = 2,
        XY = 3,
        XZ = 4,
        YZ = 5,
        XYZ = 6
    };

#pragma warning disable 0659
    sealed class VFXAttributeExpression : VFXExpression
    {
        public VFXAttributeExpression(VFXAttribute attribute, VFXAttributeLocation location = VFXAttributeLocation.Current) : base(Flags.PerElement)
        {
            m_attribute = attribute;
            m_attributeLocation = location;
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.None;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return m_attribute.type;
            }
        }

        public string attributeName
        {
            get
            {
                return m_attribute.name;
            }
        }

        public VFXAttributeLocation attributeLocation
        {
            get
            {
                return m_attributeLocation;
            }
        }

        public VFXAttribute attribute { get { return m_attribute; } }
        private VFXAttribute m_attribute;
        private VFXAttributeLocation m_attributeLocation;

        public override bool Equals(object obj)
        {
            if (!(obj is VFXAttributeExpression))
                return false;

            var other = (VFXAttributeExpression)obj;
            return valueType == other.valueType && attributeLocation == other.attributeLocation && attributeName == other.attributeName;
        }

        protected override int GetInnerHashCode()
        {
            return (attributeName.GetHashCode() * 397) ^ attributeLocation.GetHashCode();
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        public override string GetCodeString(string[] parents)
        {
            return attributeLocation == VFXAttributeLocation.Current ? attributeName : attributeName + "_source";
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            yield return new VFXAttributeInfo(attribute, m_attributeLocation == VFXAttributeLocation.Source ? VFXAttributeMode.ReadSource : VFXAttributeMode.Read);
        }
    }

    #pragma warning restore 0659
}
