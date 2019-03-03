using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// A wrapper on top of <see cref="AnimationCurve"/> to handle zero-key curves and keyframe
    /// loops.
    /// </summary>
    [Serializable]
    public sealed class Spline
    {
        /// <summary>
        /// Precision of the curve.
        /// </summary>
        public const int k_Precision = 128;

        /// <summary>
        /// The inverse of the precision of the curve.
        /// </summary>
        public const float k_Step = 1f / k_Precision;

        /// <summary>
        /// The underlying animation curve instance.
        /// </summary>
        public AnimationCurve curve;

        [SerializeField]
        bool m_Loop;

        [SerializeField]
        float m_ZeroValue;

        [SerializeField]
        float m_Range;

        AnimationCurve m_InternalLoopingCurve;

        // Used to track frame changes for data caching
        int frameCount = -1;

        /// <summary>
        /// An array holding pre-computed curve values.
        /// </summary>
        public float[] cachedData;

        /// <summary>
        /// Creates a new spline.
        /// </summary>
        /// <param name="curve">The animation curve to base this spline off</param>
        /// <param name="zeroValue">The value to return when the curve has no keyframe</param>
        /// <param name="loop">Should this curve loop?</param>
        /// <param name="bounds">The curve bounds</param>
        public Spline(AnimationCurve curve, float zeroValue, bool loop, Vector2 bounds)
        {
            Assert.IsNotNull(curve);
            this.curve = curve;
            m_ZeroValue = zeroValue;
            m_Loop = loop;
            m_Range = bounds.magnitude;
            cachedData = new float[k_Precision];
        }

        /// <summary>
        /// Caches the curve data at a given frame. The curve data will only be cached once per
        /// frame.
        /// </summary>
        /// <param name="frame">A frame number</param>
        public void Cache(int frame)
        {
            // Note: it would be nice to have a way to check if a curve has changed in any way, that
            // would save quite a few CPU cycles instead of having to force cache it once per frame :/

            // Only cache once per frame
            if (frame == frameCount)
                return;

            var length = curve.length;

            if (m_Loop && length > 1)
            {
                if (m_InternalLoopingCurve == null)
                    m_InternalLoopingCurve = new AnimationCurve();

                var prev = curve[length - 1];
                prev.time -= m_Range;
                var next = curve[0];
                next.time += m_Range;
                m_InternalLoopingCurve.keys = curve.keys;
                m_InternalLoopingCurve.AddKey(prev);
                m_InternalLoopingCurve.AddKey(next);
            }

            for (int i = 0; i < k_Precision; i++)
                cachedData[i] = Evaluate((float)i * k_Step, length);

            frameCount = Time.renderedFrameCount;
        }

        /// <summary>
        /// Evaluates the curve at a point in time.
        /// </summary>
        /// <param name="t">The time to evaluate</param>
        /// <param name="length">The number of keyframes in the curve</param>
        /// <returns>The value of the curve at time <paramref name="t"/></returns>
        public float Evaluate(float t, int length)
        {
            if (length == 0)
                return m_ZeroValue;

            if (!m_Loop || length == 1)
                return curve.Evaluate(t);

            return m_InternalLoopingCurve.Evaluate(t);
        }

        /// <summary>
        /// Evaluates the curve at a point in time.
        /// </summary>
        /// <param name="t">The time to evaluate</param>
        /// <returns>The value of the curve at time <paramref name="t"/></returns>
        /// <remarks>
        /// Calling the length getter on a curve is expensive to it's better to cache its length and
        /// call <see cref="Evaluate(float,int)"/> instead of getting the length for every call.
        /// </remarks>
        public float Evaluate(float t)
        {
            // Calling the length getter on a curve is expensive (!?) so it's better to cache its
            // length and call Evaluate(t, length) instead of getting the length for every call to
            // Evaluate(t)
            return Evaluate(t, curve.length);
        }

        /// <summary>
        /// Returns the computed hash code for this parameter.
        /// </summary>
        /// <returns>A computed hash code</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + curve.GetHashCode(); // Not implemented in Unity, so it'll always return the same value :(
                return hash;
            }
        }
    }
}
