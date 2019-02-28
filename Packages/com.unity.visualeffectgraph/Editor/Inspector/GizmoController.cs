using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public interface IGizmoable
{
    string name { get; }
}
interface IGizmoController
{
    void DrawGizmos(VisualEffect component);
    Bounds GetGizmoBounds(VisualEffect component);

    bool gizmoNeedsComponent { get; }
    bool gizmoIndeterminate { get; }

    ReadOnlyCollection<IGizmoable> gizmoables { get; }

    IGizmoable currentGizmoable { get; set; }
}
