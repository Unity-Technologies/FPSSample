using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXSpawnerBurstVariantCollection : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "repeat", Enum.GetValues(typeof(VFXSpawnerBurst.RepeatMode)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Spawn", variantProvider = typeof(VFXSpawnerBurstVariantCollection))]
    class VFXSpawnerBurst : VFXAbstractSpawner
    {
        public enum RepeatMode
        {
            Single,
            Periodic
        }

        public enum RandomMode
        {
            Constant,
            Random,
        }


        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private RepeatMode repeat = RepeatMode.Single;

        [VFXSetting, SerializeField]
        private RandomMode spawnMode =  RandomMode.Constant;

        [VFXSetting, SerializeField]
        private RandomMode delayMode = RandomMode.Constant;

        public override string name { get { return repeat.ToString() + " Burst"; } }
        public override VFXTaskType spawnerType { get { return repeat == RepeatMode.Periodic ? VFXTaskType.PeriodicBurstSpawner : VFXTaskType.BurstSpawner; } }

        public class AdvancedInputProperties
        {
            [Tooltip("Min/Max Count for each burst"), Min(0)]
            public Vector2 Count = new Vector2(0, 10);
            [Tooltip("Min/Max Delay between each burst"), Min(0)]
            public Vector2 Delay = new Vector2(0, 1);
        }

        public class SimpleInputProperties
        {
            [Tooltip("Count for each burst"), Min(0)]
            public float Count = 0.0f;
            [Tooltip("Delay between each burst"), Min(0)]
            public float Delay = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var simple = PropertiesFromType("SimpleInputProperties");
                var advanced = PropertiesFromType("AdvancedInputProperties");

                if (spawnMode == RandomMode.Constant)
                    yield return simple.FirstOrDefault(o => o.property.name == "Count");
                else
                    yield return advanced.FirstOrDefault(o => o.property.name == "Count");

                if (delayMode == RandomMode.Constant)
                    yield return simple.FirstOrDefault(o => o.property.name == "Delay");
                else
                    yield return advanced.FirstOrDefault(o => o.property.name == "Delay");
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                // Get InputProperties
                var namedExpressions = GetExpressionsFromSlots(this);

                // Map Expressions based on Task Type (TODO: Fix names on C++ side)
                string countName = repeat == RepeatMode.Periodic ? "nb" : "Count";
                string delayName = repeat == RepeatMode.Periodic ? "period" : "Delay";

                // Process Counts
                var countExp = namedExpressions.First(e => e.name == "Count").exp;

                if (spawnMode == RandomMode.Random)
                    yield return new VFXNamedExpression(countExp, countName);
                else
                    yield return new VFXNamedExpression(new VFXExpressionCombine(countExp, countExp), countName);

                // Process Delay
                var delayExp = namedExpressions.First(e => e.name == "Delay").exp;

                if (delayMode == RandomMode.Random)
                    yield return new VFXNamedExpression(delayExp, delayName);
                else
                    yield return new VFXNamedExpression(new VFXExpressionCombine(delayExp, delayExp), delayName);
            }
        }
    }
}
