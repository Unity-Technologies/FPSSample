using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class Switch : VFXOperatorDynamicBranch
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default), SerializeField]
        uint m_EntryCount = 2u;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        bool m_CustomCaseValue = false;

        public class TestInputProperties
        {
            [Tooltip("Integer value used for the test.")]
            public int testValue = 0;
        }

        public class ManualRandom
        {
            [Tooltip("Random Value")]
            public float rand = 0.0f;
        }

        public sealed override string name { get { return "Switch"; } }

        public override sealed IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 0; //TestInputProperties
                if (m_CustomCaseValue)
                {
                    var offset = 1;
                    var stride = expressionCountPerUniqueSlot + 1;
                    do
                    {
                        yield return offset;
                        offset += stride;
                    } while (offset < stride * m_EntryCount + 1);
                }
            }
        }

        protected override Type defaultValueType
        {
            get
            {
                return typeof(Color);
            }
        }


        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            if (m_EntryCount < 1) m_EntryCount = 1;
            if (m_EntryCount > 32) m_EntryCount = 32;
            base.Invalidate(model, cause);
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var baseInputProperties = base.inputProperties; //returns value is unused but there is a lazy init in input
                var manualRandomProperties = PropertiesFromType("TestInputProperties");
                foreach (var property in manualRandomProperties)
                    yield return property;

                var defaultValue = GetDefaultValueForType(GetOperandType());
                for (uint i = 0; i < m_EntryCount + 1; ++i)
                {
                    var prefix = i.ToString();
                    if (i != m_EntryCount && m_CustomCaseValue)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(int), "Case " + prefix), (int)i);
                    var name = (i == m_EntryCount) ? "default" : "Value " + prefix;
                    yield return new VFXPropertyWithValue(new VFXProperty((Type)GetOperandType(), name), defaultValue);
                }
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expressionCountPerUniqueSlot = this.expressionCountPerUniqueSlot;
            if (!m_CustomCaseValue)
            {
                //Insert Case (0,1,..) entries manually
                var newInputExpression = new VFXExpression[1 /* entry */ + m_EntryCount * (expressionCountPerUniqueSlot + 1 /* case */) + expressionCountPerUniqueSlot /* default */];

                newInputExpression[0] = inputExpression[0];
                int offsetWrite = 1;
                int offsetRead = 1;
                for (int i = 0; i < m_EntryCount + 1; ++i)
                {
                    if (i != m_EntryCount)
                        newInputExpression[offsetWrite++] = new VFXValue<int>(i);
                    for (int sub = 0; sub < expressionCountPerUniqueSlot; ++sub)
                    {
                        newInputExpression[offsetWrite++] = inputExpression[offsetRead++];
                    }
                }
                inputExpression = newInputExpression;
            }

            var referenceValue = inputExpression.First();
            referenceValue = new VFXExpressionCastIntToFloat(referenceValue);

            var startCaseOffset = 1;
            var stride = expressionCountPerUniqueSlot + 1;
            var compare = new VFXExpression[m_EntryCount];
            int offsetCase = startCaseOffset;

            var valueStartIndex = new int[m_EntryCount + 1];
            for (uint i = 0; i < m_EntryCount; i++)
            {
                valueStartIndex[i] = offsetCase + 1;
                compare[i] = new VFXExpressionCondition(VFXCondition.Equal, referenceValue, new VFXExpressionCastIntToFloat(inputExpression[offsetCase]));
                offsetCase += stride;
            }

            valueStartIndex[m_EntryCount] = inputExpression.Length - expressionCountPerUniqueSlot; //Last is default value, without a case
            return ChainedBranchResult(compare, inputExpression, valueStartIndex);
        }
    }
}
