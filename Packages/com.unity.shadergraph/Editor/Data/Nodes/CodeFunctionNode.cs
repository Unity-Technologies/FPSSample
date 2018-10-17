using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public abstract class CodeFunctionNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
    {
        [NonSerialized]
        private List<SlotAttribute> m_Slots = new List<SlotAttribute>();

        public override bool hasPreview
        {
            get { return true; }
        }

        protected CodeFunctionNode()
        {
            UpdateNodeAfterDeserialization();
        }

        protected struct Boolean
        {}

        protected struct Vector1
        {}

        protected struct Texture2D
        {}

        protected struct Texture2DArray
        {}

        protected struct Texture3D
        {}

        protected struct SamplerState
        {}

        protected struct Gradient
        {}

        protected struct DynamicDimensionVector
        {}

        protected struct ColorRGBA
        {}

        protected struct ColorRGB
        {}

        protected struct Matrix3x3
        {}

        protected struct Matrix2x2
        {}

        protected struct DynamicDimensionMatrix
        {}

        protected enum Binding
        {
            None,
            ObjectSpaceNormal,
            ObjectSpaceTangent,
            ObjectSpaceBitangent,
            ObjectSpacePosition,
            ViewSpaceNormal,
            ViewSpaceTangent,
            ViewSpaceBitangent,
            ViewSpacePosition,
            WorldSpaceNormal,
            WorldSpaceTangent,
            WorldSpaceBitangent,
            WorldSpacePosition,
            TangentSpaceNormal,
            TangentSpaceTangent,
            TangentSpaceBitangent,
            TangentSpacePosition,
            MeshUV0,
            MeshUV1,
            MeshUV2,
            MeshUV3,
            ScreenPosition,
            ObjectSpaceViewDirection,
            ViewSpaceViewDirection,
            WorldSpaceViewDirection,
            TangentSpaceViewDirection,
            VertexColor,
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        protected class SlotAttribute : Attribute
        {
            public int slotId { get; private set; }
            public Binding binding { get; private set; }
            public bool hidden { get; private set; }
            public Vector4? defaultValue { get; private set; }
            public ShaderStageCapability stageCapability { get; private set; }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, ShaderStageCapability mStageCapability = ShaderStageCapability.All)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                defaultValue = null;
                stageCapability = mStageCapability;
            }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, bool mHidden, ShaderStageCapability mStageCapability = ShaderStageCapability.All)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                hidden = mHidden;
                defaultValue = null;
                stageCapability = mStageCapability;
            }

            public SlotAttribute(int mSlotId, Binding mImplicitBinding, float defaultX, float defaultY, float defaultZ, float defaultW, ShaderStageCapability mStageCapability = ShaderStageCapability.All)
            {
                slotId = mSlotId;
                binding = mImplicitBinding;
                defaultValue = new Vector4(defaultX, defaultY, defaultZ, defaultW);
                stageCapability = mStageCapability;
            }
        }

        protected abstract MethodInfo GetFunctionToConvert();

        private static SlotValueType ConvertTypeToSlotValueType(ParameterInfo p)
        {
            Type t = p.ParameterType;
            if (p.ParameterType.IsByRef)
                t = p.ParameterType.GetElementType();

            if (t == typeof(Boolean))
            {
                return SlotValueType.Boolean;
            }
            if (t == typeof(Vector1))
            {
                return SlotValueType.Vector1;
            }
            if (t == typeof(Vector2))
            {
                return SlotValueType.Vector2;
            }
            if (t == typeof(Vector3))
            {
                return SlotValueType.Vector3;
            }
            if (t == typeof(Vector4))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(Color))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(ColorRGBA))
            {
                return SlotValueType.Vector4;
            }
            if (t == typeof(ColorRGB))
            {
                return SlotValueType.Vector3;
            }
            if (t == typeof(Texture2D))
            {
                return SlotValueType.Texture2D;
            }
            if (t == typeof(Texture2DArray))
            {
                return SlotValueType.Texture2DArray;
            }
            if (t == typeof(Texture3D))
            {
                return SlotValueType.Texture3D;
            }
            if (t == typeof(Cubemap))
            {
                return SlotValueType.Cubemap;
            }
            if (t == typeof(Gradient))
            {
                return SlotValueType.Gradient;
            }
            if (t == typeof(SamplerState))
            {
                return SlotValueType.SamplerState;
            }
            if (t == typeof(DynamicDimensionVector))
            {
                return SlotValueType.DynamicVector;
            }
            if (t == typeof(Matrix4x4))
            {
                return SlotValueType.Matrix4;
            }
            if (t == typeof(Matrix3x3))
            {
                return SlotValueType.Matrix3;
            }
            if (t == typeof(Matrix2x2))
            {
                return SlotValueType.Matrix2;
            }
            if (t == typeof(DynamicDimensionMatrix))
            {
                return SlotValueType.DynamicMatrix;
            }
            throw new ArgumentException("Unsupported type " + t);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var method = GetFunctionToConvert();

            if (method == null)
                throw new ArgumentException("Mapped method is null on node" + this);

            if (method.ReturnType != typeof(string))
                throw new ArgumentException("Mapped function should return string");

            // validate no duplicates
            var slotAtributes = method.GetParameters().Select(GetSlotAttribute).ToList();
            if (slotAtributes.Any(x => x == null))
                throw new ArgumentException("Missing SlotAttribute on " + method.Name);

            if (slotAtributes.GroupBy(x => x.slotId).Any(x => x.Count() > 1))
                throw new ArgumentException("Duplicate SlotAttribute on " + method.Name);

            List<MaterialSlot> slots = new List<MaterialSlot>();
            foreach (var par in method.GetParameters())
            {
                var attribute = GetSlotAttribute(par);
                var name = GraphUtil.ConvertCamelCase(par.Name, true);

                MaterialSlot s;
                if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(Color))
                    s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGBA))
                    s = new ColorRGBAMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                else if (attribute.binding == Binding.None && !par.IsOut && par.ParameterType == typeof(ColorRGB))
                    s = new ColorRGBMaterialSlot(attribute.slotId, name, par.Name, SlotType.Input, attribute.defaultValue ?? Vector4.zero, ColorMode.Default, stageCapability: attribute.stageCapability, hidden: attribute.hidden);
                else if (attribute.binding == Binding.None || par.IsOut)
                    s = MaterialSlot.CreateMaterialSlot(
                            ConvertTypeToSlotValueType(par),
                            attribute.slotId,
                            name,
                            par.Name,
                            par.IsOut ? SlotType.Output : SlotType.Input,
                            attribute.defaultValue ?? Vector4.zero,
                            shaderStageCapability: attribute.stageCapability,
                            hidden: attribute.hidden);
                else
                    s = CreateBoundSlot(attribute.binding, attribute.slotId, name, par.Name, attribute.stageCapability, attribute.hidden);
                slots.Add(s);

                m_Slots.Add(attribute);
            }
            foreach (var slot in slots)
            {
                AddSlot(slot);
            }
            RemoveSlotsNameNotMatching(slots.Select(x => x.id));
        }

        private static MaterialSlot CreateBoundSlot(Binding attributeBinding, int slotId, string displayName, string shaderOutputName, ShaderStageCapability shaderStageCapability, bool hidden)
        {
            switch (attributeBinding)
            {
                case Binding.ObjectSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ObjectSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ObjectSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ObjectSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ViewSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.ViewSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.ViewSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.ViewSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.WorldSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.WorldSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.WorldSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.WorldSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.TangentSpaceNormal:
                    return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.TangentSpaceTangent:
                    return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.TangentSpaceBitangent:
                    return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.TangentSpacePosition:
                    return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.MeshUV0:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV0, shaderStageCapability);
                case Binding.MeshUV1:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV1, shaderStageCapability);
                case Binding.MeshUV2:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV2, shaderStageCapability);
                case Binding.MeshUV3:
                    return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV3, shaderStageCapability);
                case Binding.ScreenPosition:
                    return new ScreenPositionMaterialSlot(slotId, displayName, shaderOutputName, ScreenSpaceType.Default, shaderStageCapability);
                case Binding.ObjectSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability);
                case Binding.ViewSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability);
                case Binding.WorldSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability);
                case Binding.TangentSpaceViewDirection:
                    return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability);
                case Binding.VertexColor:
                    return new VertexColorMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability);
                default:
                    throw new ArgumentOutOfRangeException("attributeBinding", attributeBinding, null);
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var outSlot in s_TempSlots)
            {
                visitor.AddShaderChunk(GetParamTypeName(outSlot) + " " + GetVariableNameForSlot(outSlot.id) + ";", true);
            }

            string call = GetFunctionName() + "(";
            bool first = true;
            s_TempSlots.Clear();
            GetSlots(s_TempSlots);
            s_TempSlots.Sort((slot1, slot2) => slot1.id.CompareTo(slot2.id));
            foreach (var slot in s_TempSlots)
            {
                if (!first)
                {
                    call += ", ";
                }
                first = false;

                if (slot.isInputSlot)
                    call += GetSlotValue(slot.id, generationMode);
                else
                    call += GetVariableNameForSlot(slot.id);
            }
            call += ");";

            visitor.AddShaderChunk(call, true);
        }

        private string GetParamTypeName(MaterialSlot slot)
        {
            return NodeUtils.ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
        }

        private string GetFunctionName()
        {
            var function = GetFunctionToConvert();
            return function.Name + "_" + (function.IsStatic ? string.Empty : GuidEncoder.Encode(guid) + "_") + precision 
                + (this.GetSlots<DynamicVectorMaterialSlot>().Select(s => NodeUtils.GetSlotDimension(s.concreteValueType)).FirstOrDefault() ?? "")
                + (this.GetSlots<DynamicMatrixMaterialSlot>().Select(s => NodeUtils.GetSlotDimension(s.concreteValueType)).FirstOrDefault() ?? "");
        }

        private string GetFunctionHeader()
        {
            string header = "void " + GetFunctionName() + "(";

            s_TempSlots.Clear();
            GetSlots(s_TempSlots);
            s_TempSlots.Sort((slot1, slot2) => slot1.id.CompareTo(slot2.id));
            var first = true;
            foreach (var slot in s_TempSlots)
            {
                if (!first)
                    header += ", ";

                first = false;

                if (slot.isOutputSlot)
                    header += "out ";

                header += GetParamTypeName(slot) + " " + slot.shaderOutputName;
            }

            header += ")";
            return header;
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private string GetFunctionBody(MethodInfo info)
        {
            var args = new List<object>();
            foreach (var param in info.GetParameters())
                args.Add(GetDefault(param.ParameterType));

            var result = info.Invoke(this, args.ToArray()) as string;

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            result = result.Replace("{precision}", precision.ToString());
            s_TempSlots.Clear();
            GetSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                var toReplace = string.Format("{{slot{0}dimension}}", slot.id);
                var replacement = NodeUtils.GetSlotDimension(slot.concreteValueType);
                result = result.Replace(toReplace, replacement);
            }
            return result;
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine(GetFunctionHeader());
                    var functionBody = GetFunctionBody(GetFunctionToConvert());
                    var lines = functionBody.Trim('\r', '\n', '\t', ' ');
                    s.AppendLines(lines);
                });
        }

        private static SlotAttribute GetSlotAttribute([NotNull] ParameterInfo info)
        {
            var attrs = info.GetCustomAttributes(typeof(SlotAttribute), false).OfType<SlotAttribute>().ToList();
            return attrs.FirstOrDefault();
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            var binding = NeededCoordinateSpace.None;
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresNormal();
            return binding;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            var binding = NeededCoordinateSpace.None;
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresViewDirection();
            return binding;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var binding = NeededCoordinateSpace.None;
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresPosition();
            return binding;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var binding = NeededCoordinateSpace.None;
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresTangent();
            return binding;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var binding = NeededCoordinateSpace.None;
            foreach (var slot in s_TempSlots)
                binding |= slot.RequiresBitangent();
            return binding;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresScreenPosition())
                    return true;
            }
            return false;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresVertexColor())
                    return true;
            }
            return false;
        }
    }
}
