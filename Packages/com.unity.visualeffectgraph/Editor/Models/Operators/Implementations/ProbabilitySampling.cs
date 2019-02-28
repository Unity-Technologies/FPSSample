using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class ProbabilitySampling : VFXOperatorDynamicBranch
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Use integrated random function"), SerializeField]
        private bool m_IntegratedRandom = true;

        [VFXSetting, Tooltip("Generate a random number for each particle, or one that is shared by the whole system."), SerializeField]
        public Random.SeedMode m_Seed = Random.SeedMode.PerParticle;

        [VFXSetting, Tooltip("The random number may either remain constant, or change every time it is evaluated."), SerializeField]
        public bool m_Constant = true;

        [VFXSetting, SerializeField]
        uint m_EntryCount = 3u;

        public class ConstantInputProperties
        {
            [Tooltip("An optional additional hash.")]
            public uint hash = 0u;
        }

        public class ManualRandomProperties
        {
            [Tooltip("Random Value")]
            public float rand = 0.0f;
        }

        public sealed override string name { get { return "Probability Sampling"; } }

        public override sealed IEnumerable<int> staticSlotIndex
        {
            get
            {
                var stride = expressionCountPerUniqueSlot + 1;
                for (int i = 0; i < m_EntryCount; ++i)
                    yield return i * stride;

                if (m_Constant || !m_IntegratedRandom)
                    yield return stride * (int)m_EntryCount;
            }
        }

        protected override Type defaultValueType
        {
            get
            {
                return typeof(Color);
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!m_IntegratedRandom)
                {
                    yield return "m_Seed";
                    yield return "m_Constant";
                }
            }
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            if (m_EntryCount < 2) m_EntryCount = 2;
            if (m_EntryCount > 32) m_EntryCount = 32;
            base.Invalidate(model, cause);
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseInputProperties = base.inputProperties;
                var defaultValue = GetDefaultValueForType(GetOperandType());
                for (uint i = 0; i < m_EntryCount; ++i)
                {
                    var prefix = i.ToString();
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Weight " + prefix), 1.0f);
                    yield return new VFXPropertyWithValue(new VFXProperty((Type)GetOperandType(), "Value " + prefix), defaultValue);
                }

                if (m_IntegratedRandom)
                {
                    if (m_Constant)
                    {
                        var constantProperties = PropertiesFromType("ConstantInputProperties");
                        foreach (var property in constantProperties)
                            yield return property;
                    }
                }
                else
                {
                    var manualRandomProperties = PropertiesFromType("ManualRandomProperties");
                    foreach (var property in manualRandomProperties)
                        yield return property;
                }
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression rand = null;
            if (m_IntegratedRandom)
            {
                if (m_Constant)
                    rand = VFXOperatorUtility.FixedRandom(inputExpression.Last(), m_Seed == Random.SeedMode.PerParticle);
                else
                    rand = new VFXExpressionRandom(m_Seed == Random.SeedMode.PerParticle);
            }
            else
            {
                rand = inputExpression.Last();
            }

            var expressionCountPerUniqueSlot = this.expressionCountPerUniqueSlot;

            var stride = expressionCountPerUniqueSlot + 1;
            var prefixedProbablities = new VFXExpression[m_EntryCount];
            int offsetProbabilities = 0;
            prefixedProbablities[0] = inputExpression[offsetProbabilities];
            for (uint i = 1; i < m_EntryCount; i++)
            {
                offsetProbabilities += stride;
                prefixedProbablities[i] = prefixedProbablities[i - 1] + inputExpression[offsetProbabilities];
            }
            rand = rand * prefixedProbablities.Last();

            var compare = new VFXExpression[m_EntryCount - 1];
            for (int i = 0; i < m_EntryCount - 1; i++)
            {
                compare[i] = new VFXExpressionCondition(VFXCondition.GreaterOrEqual, prefixedProbablities[i], rand);
            };

            var startValueIndex = Enumerable.Range(0, (int)m_EntryCount).Select(o => o * stride + 1).ToArray();
            return ChainedBranchResult(compare, inputExpression, startValueIndex);
        }
    }
}
