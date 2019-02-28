using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class VFXTypeAttribute : Attribute
    {}

    public enum SpaceableType
    {
        None,
        Position,
        Direction,
        Matrix,
        Vector
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct)]
    public class VFXSpaceAttribute : PropertyAttribute
    {
        public readonly SpaceableType type;
        public VFXSpaceAttribute(SpaceableType type)
        {
            this.type = type;
        }
    }

    public class ShowAsColorAttribute : Attribute
    {}

    class CoordinateSpaceInfo
    {
        public static readonly int SpaceCount = Enum.GetValues(typeof(VFXCoordinateSpace)).Length;
    }

    [VFXType, Serializable]
    struct Circle
    {
        [Tooltip("The centre of the circle."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The radius of the circle.")]
        public float radius;

        public static Circle defaultValue = new Circle { radius = 1.0f };
    }

    [VFXType, Serializable]
    struct ArcCircle
    {
        public Circle circle;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the circle is used.")]
        public float arc;

        public static ArcCircle defaultValue = new ArcCircle { circle = Circle.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType, Serializable]
    struct Sphere
    {
        [Tooltip("The centre of the sphere."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The radius of the sphere.")]
        public float radius;

        public static Sphere defaultValue = new Sphere { radius = 1.0f };
    }

    [VFXType, Serializable]
    struct ArcSphere
    {
        public Sphere sphere;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the sphere is used.")]
        public float arc;

        public static ArcSphere defaultValue = new ArcSphere { sphere = Sphere.defaultValue, arc = 2.0f * Mathf.PI };
    }

    [VFXType, VFXSpace(SpaceableType.Matrix), Serializable]
    struct OrientedBox
    {
        [Tooltip("The centre of the box."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Angle, Tooltip("The orientation of the box.")]
        public Vector3 angles;
        [Tooltip("The size of the box along each axis.")]
        public Vector3 size;

        public static OrientedBox defaultValue = new OrientedBox { size = Vector3.one };
    }

    [VFXType, Serializable]
    struct AABox
    {
        [Tooltip("The centre of the box."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The size of the box along each axis.")]
        public Vector3 size;

        public static AABox defaultValue = new AABox { size = Vector3.one };
    }

    [VFXType, Serializable]
    struct Plane
    {
        public Plane(Vector3 direction) { position = Vector3.zero; normal = direction; }

        [Tooltip("The position of the plane."), VFXSpace(SpaceableType.Position)]
        public Vector3 position;
        [Normalize, Tooltip("The direction of the plane."), VFXSpace(SpaceableType.Direction)]
        public Vector3 normal;

        public static Plane defaultValue = new Plane { normal = Vector3.up };
    }

    [VFXType, Serializable]
    struct Cylinder
    {
        [Tooltip("The center of the cylinder."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The radius of the cylinder.")]
        public float radius;
        [Tooltip("The height of the cylinder.")]
        public float height;

        public static Cylinder defaultValue = new Cylinder { radius = 1.0f, height = 1.0f };
    }

    [VFXType, Serializable]
    struct Cone
    {
        [Tooltip("The center of the cone."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The first radius of the cone.")]
        public float radius0;
        [Tooltip("The second radius of the cone.")]
        public float radius1;
        [Tooltip("The height of the cone.")]
        public float height;

        public static Cone defaultValue = new Cone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f };
    }

    [VFXType, Serializable]
    struct ArcCone
    {
        [Tooltip("The center of the cone."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The first radius of the cone.")]
        public float radius0;
        [Tooltip("The second radius of the cone.")]
        public float radius1;
        [Tooltip("The height of the cone.")]
        public float height;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the cone is used.")]
        public float arc;

        public static ArcCone defaultValue = new ArcCone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f, arc = Mathf.PI / 3.0f };
    }

    [VFXType, Serializable]
    struct Torus
    {
        [Tooltip("The centre of the torus."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("The thickness of the torus ring.")]
        public float minorRadius;

        public static Torus defaultValue = new Torus { majorRadius = 1.0f, minorRadius = 0.1f };
    }

    [VFXType, Serializable]
    struct ArcTorus
    {
        [Tooltip("The centre of the torus."), VFXSpace(SpaceableType.Position)]
        public Vector3 center;
        [Tooltip("The radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("The thickness of the torus ring.")]
        public float minorRadius;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the torus is used.")]
        public float arc;

        public static ArcTorus defaultValue = new ArcTorus { majorRadius = 1.0f, minorRadius = 0.1f, arc = Mathf.PI / 3.0f };
    }

    [VFXType, Serializable]
    struct Line
    {
        [Tooltip("The start position of the line."), VFXSpace(SpaceableType.Position)]
        public Vector3 start;
        [Tooltip("The end position of the line."), VFXSpace(SpaceableType.Position)]
        public Vector3 end;

        public static Line defaultValue = new Line { start = Vector3.zero, end = Vector3.left };
    }

    [VFXType, VFXSpace(SpaceableType.Matrix), Serializable]
    struct Transform
    {
        [Tooltip("The transform position."), VFXSpace(SpaceableType.Position)]
        public Vector3 position;
        [Angle, Tooltip("The euler angles of the transform.")]
        public Vector3 angles;
        [Tooltip("The scale of the transform along each axis.")]
        public Vector3 scale;

        public static Transform defaultValue = new Transform { scale = Vector3.one };
    }

    [VFXType, Serializable]
    struct Position
    {
        [Tooltip("The position."), VFXSpace(SpaceableType.Position)]
        public Vector3 position;

        public static implicit operator Position(Vector3 v)
        {
            return new Position() { position = v };
        }

        public static implicit operator Vector3(Position v)
        {
            return v.position;
        }

        public static Position defaultValue = new Position { position = Vector3.zero };
    }

    [VFXType, Serializable]
    struct DirectionType
    {
        [Tooltip("The normalized direction."), VFXSpace(SpaceableType.Direction)]
        public Vector3 direction;

        public static implicit operator DirectionType(Vector3 v)
        {
            return new DirectionType() { direction = v };
        }

        public static implicit operator Vector3(DirectionType v)
        {
            return v.direction;
        }

        public static DirectionType defaultValue = new DirectionType { direction = Vector3.up };
    }

    [VFXType, Serializable]
    struct Vector
    {
        [Tooltip("The vector."), VFXSpace(SpaceableType.Vector)]
        public Vector3 vector;

        public static implicit operator Vector(Vector3 v)
        {
            return new Vector() { vector = v };
        }

        public static implicit operator Vector3(Vector v)
        {
            return v.vector;
        }

        public static Vector defaultValue = new Vector { vector = Vector3.zero };
    }

    [VFXType, Serializable]
    struct FlipBook
    {
        public int x;
        public int y;

        public static FlipBook defaultValue = new FlipBook { x = 4, y = 4 };
    }

    [VFXType, Serializable]
    struct CameraType
    {
        [Tooltip("The camera's Transform in the world.")]
        public Transform transform;
        [Angle, Range(0.0f, Mathf.PI), Tooltip("The field of view.")]
        public float fieldOfView;
        [Min(0.0f), Tooltip("The near plane.")]
        public float nearPlane;
        [Min(0.0f), Tooltip("The far plane.")]
        public float farPlane;
        [Min(0.0f), Tooltip("The aspect ratio.")]
        public float aspectRatio;
        [Min(0.0f), Tooltip("The width and height of the camera in pixels.")]
        public Vector2 pixelDimensions;

        public static CameraType defaultValue = new CameraType { transform = Transform.defaultValue, fieldOfView = 60.0f * Mathf.Deg2Rad, nearPlane = 0.3f, farPlane = 1000.0f, aspectRatio = 1.0f, pixelDimensions = new Vector2(1920, 1080) };
    }
}
