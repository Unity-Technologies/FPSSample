using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block.Test
{
    class AllType : VFXBlock
    {
        public override string name                         { get { return "Test"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kAll; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle | VFXDataType.kSpawnEvent; } }

        public class InputProperties
        {
            [Min(2.5f)]
            public float aFloat = 123.456f;
            [Range(-32, 150)]
            public float aRange = 123.456f;
            public Gradient aGradient = new Gradient();
            public Vector2 aVector2 = Vector2.left;
            [Normalize]
            public Vector3 aVector3 = Vector3.forward;
            public Vector4 aVector4 = Vector4.one;
            public Transform aTransform = new Transform();
            [Tooltip("This is a color")]
            public Color aColor = Color.gray;
            public Texture2D aTexture2D = null;
            public Texture2DArray aTexture2DArray = null;
            public Texture3D aTexture3D = null;
            public Cubemap aTextureCube = null;
            public CubemapArray aTextureCubeArray = null;
            public Sphere aSphere = new Sphere();
            public Vector aVector = new Vector { vector = Vector3.one };
            public Position aPosition = new Position { position = Vector3.forward };
            public int anInt = 123;
            public uint anUint = 321;
            public FlipBook aFlipBook = new FlipBook();
            public bool aBool = true;
            public AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1)});
            public Mesh aMesh = null;
        }


        public enum RotateMode
        {
            Quaternion,
            Euler
        }

        [VFXSetting]
        public RotateMode mode;
        [VFXSetting]
        public bool rotateTwice;
    }
}
