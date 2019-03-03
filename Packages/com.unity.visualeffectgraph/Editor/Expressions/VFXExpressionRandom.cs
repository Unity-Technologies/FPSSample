using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Runtime.CompilerServices;

namespace UnityEditor.VFX
{
    #pragma warning disable 0659
    class VFXExpressionRandom : VFXExpression
    {
        public VFXExpressionRandom(bool perElement = false) : base(perElement ? VFXExpression.Flags.PerElement : VFXExpression.Flags.None)
        {}

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        protected override int GetInnerHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.GenerateRandom; } }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return VFXValue.Constant(UnityEngine.Random.value);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("RAND");
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            if (Is(Flags.PerElement))
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
        }
    }

    class VFXExpressionFixedRandom : VFXExpression
    {
        public VFXExpressionFixedRandom() : this(VFXValue<uint>.Default) {}
        public VFXExpressionFixedRandom(VFXExpression hash, bool perElement = false) : base(perElement ? VFXExpression.Flags.PerElement : VFXExpression.Flags.None, hash)
        {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.GenerateFixedRandom; }}

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState((int)constParents[0].Get<uint>());

            var result = VFXValue.Constant(UnityEngine.Random.value);

            UnityEngine.Random.state = oldState;

            return result;
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("FixedRand(particleId ^ {0})", parents[0]);
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            if (Is(Flags.PerElement))
                yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
        }
    }
    #pragma warning restore 0659
}
