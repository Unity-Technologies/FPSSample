using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionValueNoise1D : VFXExpression
    {
        public VFXExpressionValueNoise1D() : this(VFXValue<float>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionValueNoise1D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<float>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueNoise1D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionValueNoise2D : VFXExpression
    {
        public VFXExpressionValueNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionValueNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueNoise2D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionValueNoise3D : VFXExpression
    {
        public VFXExpressionValueNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionValueNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueNoise3D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinNoise1D : VFXExpression
    {
        public VFXExpressionPerlinNoise1D() : this(VFXValue<float>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionPerlinNoise1D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<float>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinNoise1D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinNoise2D : VFXExpression
    {
        public VFXExpressionPerlinNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionPerlinNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinNoise2D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinNoise3D : VFXExpression
    {
        public VFXExpressionPerlinNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionPerlinNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinNoise3D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionCellularNoise1D : VFXExpression
    {
        public VFXExpressionCellularNoise1D() : this(VFXValue<float>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionCellularNoise1D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<float>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateCellularNoise1D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateCellularNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionCellularNoise2D : VFXExpression
    {
        public VFXExpressionCellularNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionCellularNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateCellularNoise2D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateCellularNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionCellularNoise3D : VFXExpression
    {
        public VFXExpressionCellularNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionCellularNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateCellularNoise3D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateCellularNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionValueCurlNoise2D : VFXExpression
    {
        public VFXExpressionValueCurlNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) { }
        public VFXExpressionValueCurlNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) { }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueCurlNoise2D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueCurlNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionValueCurlNoise3D : VFXExpression
    {
        public VFXExpressionValueCurlNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) { }
        public VFXExpressionValueCurlNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) { }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueCurlNoise3D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueCurlNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinCurlNoise2D : VFXExpression
    {
        public VFXExpressionPerlinCurlNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) { }
        public VFXExpressionPerlinCurlNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) { }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinCurlNoise2D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinCurlNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinCurlNoise3D : VFXExpression
    {
        public VFXExpressionPerlinCurlNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) { }
        public VFXExpressionPerlinCurlNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) { }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinCurlNoise3D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinCurlNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionCellularCurlNoise2D : VFXExpression
    {
        public VFXExpressionCellularCurlNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) { }
        public VFXExpressionCellularCurlNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) { }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateCellularCurlNoise2D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateCellularCurlNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionCellularCurlNoise3D : VFXExpression
    {
        public VFXExpressionCellularCurlNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) { }
        public VFXExpressionCellularCurlNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) { }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateCellularCurlNoise3D(coordinate, floatParams.x, octaveCount, floatParams.y, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateCellularCurlNoise({0}, {1}.x, {2}, {1}.y, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionVoroNoise2D : VFXExpression
    {
        public VFXExpressionVoroNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default) {}
        public VFXExpressionVoroNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateVoroNoise2D(coordinate, floatParams));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateVoroNoise({0}, {1}.y, {1}.x, {1}.z)", parents[0], parents[1]);
        }
    }
}
