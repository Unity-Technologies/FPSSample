using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class DecalProjectorComponentHandle : PrimitiveBoundsHandle
    {
        public DecalProjectorComponentHandle() : base()
        {
            midpointHandleDrawFunction = DrawHandleMidpoint;
        }

        public UnityEngine.Vector3 size { get { return GetSize(); } set { SetSize(value); } }

        protected override void DrawWireframe()
        {
            Handles.DrawWireCube(center, size);
            DrawArrowDownProjectionDirection();
        }

        protected void DrawArrowDownProjectionDirection()
        {
            int controlID = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
            Quaternion arrowRotation = Quaternion.LookRotation(Vector3.down, Vector3.right);
            float arrowSize = size.y * 0.25f;
            Handles.ArrowHandleCap(controlID, center, arrowRotation, arrowSize, EventType.Repaint);
        }

        // Could use a static readonly LUT, but this would require syncing with order with enum HandleDirection.
        protected static Color ColorFromHandleDirection(HandleDirection handleDirection)
        {
            switch (handleDirection)
            {
                case HandleDirection.PositiveX:
                case HandleDirection.NegativeX:
                    return Handles.xAxisColor;

                case HandleDirection.PositiveY:
                case HandleDirection.NegativeY:
                    return Handles.yAxisColor;

                case HandleDirection.PositiveZ:
                case HandleDirection.NegativeZ:
                    return Handles.zAxisColor;

                default:
                    throw new ArgumentOutOfRangeException("handleDirection", "Must be PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, or NegativeZ");
            }
        }

        protected static void PlaneVerticesFromHandleDirection(ref Vector3[] outVertices, HandleDirection handleDirection, Vector3 boundsSize, Vector3 boundsCenter)
        {
            Vector3 boundsMin = boundsSize * -0.5f + boundsCenter;
            Vector3 boundsMax = boundsSize * 0.5f + boundsCenter;

            switch (handleDirection)
            {
                case HandleDirection.PositiveX:
                    outVertices[0] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
                    outVertices[1] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
                    outVertices[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMax.z);
                    outVertices[3] = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
                    break;

                case HandleDirection.NegativeX:
                    outVertices[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
                    outVertices[1] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
                    outVertices[2] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
                    outVertices[3] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
                    break;

                case HandleDirection.PositiveY:
                    outVertices[0] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
                    outVertices[1] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
                    outVertices[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMax.z);
                    outVertices[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
                    break;

                case HandleDirection.NegativeY:
                    outVertices[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
                    outVertices[1] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
                    outVertices[2] = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
                    outVertices[3] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
                    break;

                case HandleDirection.PositiveZ:
                    outVertices[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
                    outVertices[1] = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
                    outVertices[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMax.z);
                    outVertices[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
                    break;

                case HandleDirection.NegativeZ:
                    outVertices[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
                    outVertices[1] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
                    outVertices[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
                    outVertices[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("handleDirection", "Must be PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, or NegativeZ");
            }
        }

        // As DrawHandleDirectionPlane() is called every frame during gizmo rendering, we pre-allocate a scratch array for passing along to DrawSolidRectangleWithOutline()
        // rather than putting pressure on the garbage collector every frame. Since DrawHandleDirectionPlane() is responsible for drawing handles, we will only ever call
        // it from the main thread, so there is no realistic risk of a race condition occuring due to this static allocation.
        private static Vector3[] s_PlaneVertices = new Vector3[4];
        protected static void DrawHandleDirectionPlane(HandleDirection handleDirection, Vector3 size, Vector3 center)
        {
            // Set global Handles.color to white to avoid global state from interfering with the desired colors set at DrawSolidRectangleWithOutline().
            Color handlesColorPrevious = Handles.color;
            Handles.color = Color.white;

            Color planeColorOutline = ColorFromHandleDirection(handleDirection);
            const float planeColorFillAlpha = 0.25f;
            Color planeColorFill = planeColorOutline * planeColorFillAlpha;

            PlaneVerticesFromHandleDirection(ref s_PlaneVertices, handleDirection, size, center);
            Handles.DrawSolidRectangleWithOutline(s_PlaneVertices, planeColorFill, planeColorOutline);

            Handles.color = handlesColorPrevious;
        }

        // Utility function for determining the handle direction (which face) we are currently rendering within DrawHandleMidpoint().
        // Ideally, the base class PrimitiveBoundsHandle would expose the reverse lookup: HandleDirectionFromControlID().
        // In lieu of an explicit way to handle this look up, we derive the handle direction from the handle rotation.
        protected static HandleDirection HandleDirectionFromRotation(Quaternion rotation)
        {
            if (rotation.x == 0.0f && rotation.y == 0.7071068f && rotation.z == 0.0f && rotation.w == 0.7071068f)
            {
                return HandleDirection.PositiveX;
            }
            else if (rotation.x == 0.0f && rotation.y == -0.7071068f && rotation.z == 0.0f && rotation.w == 0.7071068f)
            {
                return HandleDirection.NegativeX;
            }
            else if (rotation.x == -0.7071068f && rotation.y == 0.0f && rotation.z == 0.0f && rotation.w == 0.7071068f)
            {
                return HandleDirection.PositiveY;
            }
            else if (rotation.x == 0.7071068f && rotation.y == 0.0f && rotation.z == 0.0f && rotation.w == 0.7071068f)
            {
                return HandleDirection.NegativeY;
            }
            else if (rotation.x == 0.0f && rotation.y == 0.0f && rotation.z == 0.0f && rotation.w == 1.0f)
            {
                return HandleDirection.PositiveZ;
            }
            else if (rotation.x == 0.0f && rotation.y == 1.0f && rotation.z == 0.0f && rotation.w == 0.0f)
            {
                return HandleDirection.NegativeZ;
            }
            else
            {
                throw new ArgumentOutOfRangeException("rotation", "Must point down PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, or NegativeZ");
            }
        }

        protected void DrawHandleMidpoint(int handleControlID, Vector3 handlePosition, Quaternion handleRotation, float handleSize, EventType eventType)
        {
            // Highlight the plane we are currently interacting with.
            if (handleControlID == GUIUtility.hotControl)
            {
                HandleDirection handleDirection = HandleDirectionFromRotation(handleRotation);
                DrawHandleDirectionPlane(handleDirection, size, center);
            }

            // Draw standard PrimitiveBoundsHandle mindpoint handle.
            Handles.DotHandleCap(handleControlID, handlePosition, handleRotation, handleSize, eventType);
        }
    }
}
