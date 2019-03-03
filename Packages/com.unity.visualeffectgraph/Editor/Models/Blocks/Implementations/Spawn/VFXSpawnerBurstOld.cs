using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Obsolete]
    class VFXSpawnerBurstOld : VFXAbstractSpawner
    {
        [VFXSetting, SerializeField]
        private bool advanced = true;

        public override string name { get { return "Burst (DEPRECATED)"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.BurstSpawner; } }

        public class AdvancedInputProperties
        {
            public Vector2 Count = new Vector2(0, 10);
            public Vector2 Delay = new Vector2(0, 1);
        }

        public class SimpleInputProperties
        {
            public float Count = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get { return PropertiesFromType(advanced ? "AdvancedInputProperties" : "SimpleInputProperties"); }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var namedExpressions = GetExpressionsFromSlots(this);
                if (advanced)
                {
                    foreach (var e in namedExpressions)
                        yield return e;
                }
                else
                {
                    var countExp = namedExpressions.First(e => e.name == "Count").exp;
                    yield return new VFXNamedExpression(new VFXExpressionCombine(countExp, countExp), "Count");
                    yield return new VFXNamedExpression(VFXValue.Constant(Vector2.zero), "Delay");
                }
            }
        }

        public override void Sanitize(int version)
        {
            var newBlock = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            newBlock.SetSettingValue("repeat", VFXSpawnerBurst.RepeatMode.Single);


            if (advanced)
            {
                newBlock.SetSettingValue("spawnMode", VFXSpawnerBurst.RandomMode.Random);
                newBlock.SetSettingValue("delayMode", VFXSpawnerBurst.RandomMode.Random);
            }
            else
            {
                newBlock.SetSettingValue("spawnMode", VFXSpawnerBurst.RandomMode.Constant);
                newBlock.SetSettingValue("delayMode", VFXSpawnerBurst.RandomMode.Constant);
            }

            // Count
            VFXSlot.CopyLinksAndValue(newBlock.GetInputSlot(0), GetInputSlot(0), true);

            // Delay
            if (advanced)
                VFXSlot.CopyLinksAndValue(newBlock.GetInputSlot(1), GetInputSlot(1), true);
            else
                newBlock.GetInputSlot(1).value = 0.0f;

            ReplaceModel(newBlock, this);
            base.Sanitize(version);
        }
    }
}
