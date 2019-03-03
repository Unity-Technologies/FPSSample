using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionLine : PositionBase
    {
        public override string name { get { return "Position (Line)"; } }

        public class InputProperties
        {
            [Tooltip("The line used for positioning particles.")]
            public Line line = new Line() { start = Vector3.zero, end = Vector3.right };
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position along the line to emit particles from.")]
            public float LineSequencer = 0.0f;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "positionMode";
            }
        }

        public override string source
        {
            get
            {
                if (spawnMode == SpawnMode.Custom)
                    return @"position += lerp(line_start, line_end, LineSequencer);";
                else
                    return @"position += lerp(line_start, line_end, RAND);";
            }
        }
    }
}
