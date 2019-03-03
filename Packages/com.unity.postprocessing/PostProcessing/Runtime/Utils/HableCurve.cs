namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// A raw implementation of John Hable's artist-friendly tonemapping curve.
    /// See http://filmicworlds.com/blog/filmic-tonemapping-with-piecewise-power-curves/
    /// </summary>
    public class HableCurve
    {
        class Segment
        {
            public float offsetX;
            public float offsetY;
            public float scaleX;
            public float scaleY;
            public float lnA;
            public float B;

            public float Eval(float x)
            {
                float x0 = (x - offsetX) * scaleX;
                float y0 = 0f;

                // log(0) is undefined but our function should evaluate to 0. There are better ways to handle this,
                // but it's doing it the slow way here for clarity.
                if (x0 > 0)
                    y0 = Mathf.Exp(lnA + B * Mathf.Log(x0));

                return y0 * scaleY + offsetY;
            }
        }

        struct DirectParams
        {
            internal float x0;
            internal float y0;
            internal float x1;
            internal float y1;
            internal float W;

            internal float overshootX;
            internal float overshootY;

            internal float gamma;
        }

        /// <summary>
        /// The curve's white point.
        /// </summary>
        public float whitePoint { get; private set; }

        /// <summary>
        /// The inverse of the curve's white point.
        /// </summary>
        public float inverseWhitePoint { get; private set; }

        internal float x0 { get; private set; }
        internal float x1 { get; private set; }

        // Toe, mid, shoulder
        readonly Segment[] m_Segments = new Segment[3];

        /// <summary>
        /// Creates a new curve.
        /// </summary>
        public HableCurve()
        {
            for (int i = 0; i < 3; i++)
                m_Segments[i] = new Segment();

            uniforms = new Uniforms(this);
        }

        /// <summary>
        /// Evaluates a given point on the curve.
        /// </summary>
        /// <param name="x">The point within the curve to evaluate (on the horizontal axis)</param>
        /// <returns>The value of the curve, at the point specified</returns>
        public float Eval(float x)
        {
            float normX = x * inverseWhitePoint;
            int index = (normX < x0) ? 0 : ((normX < x1) ? 1 : 2);
            var segment = m_Segments[index];
            float ret = segment.Eval(normX);
            return ret;
        }

        /// <summary>
        /// Initializes the curve with given settings.
        /// </summary>
        /// <param name="toeStrength">Affects the transition between the toe and the mid section of
        /// the curve. A value of 0 means no toe, a value of 1 means a very hard transition</param>
        /// <param name="toeLength">Affects how much of the dynamic range is in the toe. With a
        /// small value, the toe will be very short and quickly transition into the linear section,
        /// and with a longer value having a longer toe</param>
        /// <param name="shoulderStrength">Affects the transition between the mid section and the
        /// shoulder of the curve. A value of 0 means no shoulder, a value of 1 means a very hard
        /// transition</param>
        /// <param name="shoulderLength">Affects how many F-stops (EV) to add to the dynamic range
        /// of the curve</param>
        /// <param name="shoulderAngle">Affects how much overshoot to add to the shoulder</param>
        /// <param name="gamma">Applies a gamma function to the curve</param>
        public void Init(float toeStrength, float toeLength, float shoulderStrength, float shoulderLength, float shoulderAngle, float gamma)
        {
            var dstParams = new DirectParams();

            // This is not actually the display gamma. It's just a UI space to avoid having to 
            // enter small numbers for the input.
            const float kPerceptualGamma = 2.2f;

            // Constraints
            {
                toeLength = Mathf.Pow(Mathf.Clamp01(toeLength), kPerceptualGamma);
                toeStrength = Mathf.Clamp01(toeStrength);
                shoulderAngle = Mathf.Clamp01(shoulderAngle);
                shoulderStrength = Mathf.Clamp(shoulderStrength, 1e-5f, 1f - 1e-5f);
                shoulderLength = Mathf.Max(0f, shoulderLength);
                gamma = Mathf.Max(1e-5f, gamma);
            }

            // Apply base params
            {
                // Toe goes from 0 to 0.5
                float x0 = toeLength * 0.5f;
                float y0 = (1f - toeStrength) * x0; // Lerp from 0 to x0

                float remainingY = 1f - y0;

                float initialW = x0 + remainingY;

                float y1_offset = (1f - shoulderStrength) * remainingY;
                float x1 = x0 + y1_offset;
                float y1 = y0 + y1_offset;

                // Filmic shoulder strength is in F stops
                float extraW = RuntimeUtilities.Exp2(shoulderLength) - 1f;

                float W = initialW + extraW;

                dstParams.x0 = x0;
                dstParams.y0 = y0;
                dstParams.x1 = x1;
                dstParams.y1 = y1;
                dstParams.W = W;

                // Bake the linear to gamma space conversion
                dstParams.gamma = gamma;
            }

            dstParams.overshootX = (dstParams.W * 2f) * shoulderAngle * shoulderLength;
            dstParams.overshootY = 0.5f * shoulderAngle * shoulderLength;

            InitSegments(dstParams);
        }

        void InitSegments(DirectParams srcParams)
        {
            var paramsCopy = srcParams;

            whitePoint = srcParams.W;
            inverseWhitePoint = 1f / srcParams.W;

            // normalize params to 1.0 range
            paramsCopy.W = 1f;
            paramsCopy.x0 /= srcParams.W;
            paramsCopy.x1 /= srcParams.W;
            paramsCopy.overshootX = srcParams.overshootX / srcParams.W;

            float toeM = 0f;
            float shoulderM = 0f;
            {
                float m, b;
                AsSlopeIntercept(out m, out b, paramsCopy.x0, paramsCopy.x1, paramsCopy.y0, paramsCopy.y1);

                float g = srcParams.gamma;

                // Base function of linear section plus gamma is
                // y = (mx+b)^g
                //
                // which we can rewrite as
                // y = exp(g*ln(m) + g*ln(x+b/m))
                //
                // and our evaluation function is (skipping the if parts):
                /*
                    float x0 = (x - offsetX) * scaleX;
                    y0 = exp(m_lnA + m_B*log(x0));
                    return y0*scaleY + m_offsetY;
                */

                var midSegment = m_Segments[1];
                midSegment.offsetX = -(b / m);
                midSegment.offsetY = 0f;
                midSegment.scaleX = 1f;
                midSegment.scaleY = 1f;
                midSegment.lnA = g * Mathf.Log(m);
                midSegment.B = g;

                toeM = EvalDerivativeLinearGamma(m, b, g, paramsCopy.x0);
                shoulderM = EvalDerivativeLinearGamma(m, b, g, paramsCopy.x1);

                // apply gamma to endpoints
                paramsCopy.y0 = Mathf.Max(1e-5f, Mathf.Pow(paramsCopy.y0, paramsCopy.gamma));
                paramsCopy.y1 = Mathf.Max(1e-5f, Mathf.Pow(paramsCopy.y1, paramsCopy.gamma));

                paramsCopy.overshootY = Mathf.Pow(1f + paramsCopy.overshootY, paramsCopy.gamma) - 1f;
            }

            this.x0 = paramsCopy.x0;
            this.x1 = paramsCopy.x1;

            // Toe section
            {
                var toeSegment = m_Segments[0];
                toeSegment.offsetX = 0;
                toeSegment.offsetY = 0f;
                toeSegment.scaleX = 1f;
                toeSegment.scaleY = 1f;

                float lnA, B;
                SolveAB(out lnA, out B, paramsCopy.x0, paramsCopy.y0, toeM);
                toeSegment.lnA = lnA;
                toeSegment.B = B;
            }

            // Shoulder section
            {
                // Use the simple version that is usually too flat 
                var shoulderSegment = m_Segments[2];

                float x0 = (1f + paramsCopy.overshootX) - paramsCopy.x1;
                float y0 = (1f + paramsCopy.overshootY) - paramsCopy.y1;

                float lnA, B;
                SolveAB(out lnA, out B, x0, y0, shoulderM);

                shoulderSegment.offsetX = (1f + paramsCopy.overshootX);
                shoulderSegment.offsetY = (1f + paramsCopy.overshootY);

                shoulderSegment.scaleX = -1f;
                shoulderSegment.scaleY = -1f;
                shoulderSegment.lnA = lnA;
                shoulderSegment.B = B;
            }

            // Normalize so that we hit 1.0 at our white point. We wouldn't have do this if we 
            // skipped the overshoot part.
            {
                // Evaluate shoulder at the end of the curve
                float scale = m_Segments[2].Eval(1f);
                float invScale = 1f / scale;

                m_Segments[0].offsetY *= invScale;
                m_Segments[0].scaleY *= invScale;

                m_Segments[1].offsetY *= invScale;
                m_Segments[1].scaleY *= invScale;

                m_Segments[2].offsetY *= invScale;
                m_Segments[2].scaleY *= invScale;
            }
        }

        // Find a function of the form:
        //   f(x) = e^(lnA + Bln(x))
        // where
        //   f(0)   = 0; not really a constraint
        //   f(x0)  = y0
        //   f'(x0) = m
        void SolveAB(out float lnA, out float B, float x0, float y0, float m)
        {
            B = (m * x0) / y0;
            lnA = Mathf.Log(y0) - B * Mathf.Log(x0);
        }

        // Convert to y=mx+b
        void AsSlopeIntercept(out float m, out float b, float x0, float x1, float y0, float y1)
        {
            float dy = (y1 - y0);
            float dx = (x1 - x0);

            if (dx == 0)
                m = 1f;
            else
                m = dy / dx;

            b = y0 - x0 * m;
        }

        // f(x) = (mx+b)^g
        // f'(x) = gm(mx+b)^(g-1)
        float EvalDerivativeLinearGamma(float m, float b, float g, float x)
        {
            float ret = g * m * Mathf.Pow(m * x + b, g - 1f);
            return ret;
        }

        /// <summary>
        /// Utility class to retrieve curve values for shader evaluation.
        /// </summary>
        public class Uniforms
        {
            HableCurve parent;

            internal Uniforms(HableCurve parent)
            {
                this.parent = parent;
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(inverseWhitePoint, x0, x1, 0)</c>.
            /// </summary>
            public Vector4 curve
            {
                get { return new Vector4(parent.inverseWhitePoint, parent.x0, parent.x1, 0f); }
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(toe.offsetX, toe.offsetY, toe.scaleX, toe.scaleY)</c>.
            /// </summary>
            public Vector4 toeSegmentA
            {
                get
                {
                    var toe = parent.m_Segments[0];
                    return new Vector4(toe.offsetX, toe.offsetY, toe.scaleX, toe.scaleY);
                }
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(toe.lnA, toe.B, 0, 0)</c>.
            /// </summary>
            public Vector4 toeSegmentB
            {
                get
                {
                    var toe = parent.m_Segments[0];
                    return new Vector4(toe.lnA, toe.B, 0f, 0f);
                }
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(mid.offsetX, mid.offsetY, mid.scaleX, mid.scaleY)</c>.
            /// </summary>
            public Vector4 midSegmentA
            {
                get
                {
                    var mid = parent.m_Segments[1];
                    return new Vector4(mid.offsetX, mid.offsetY, mid.scaleX, mid.scaleY);
                }
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(mid.lnA, mid.B, 0, 0)</c>.
            /// </summary>
            public Vector4 midSegmentB
            {
                get
                {
                    var mid = parent.m_Segments[1];
                    return new Vector4(mid.lnA, mid.B, 0f, 0f);
                }
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(toe.offsetX, toe.offsetY, toe.scaleX, toe.scaleY)</c>.
            /// </summary>
            public Vector4 shoSegmentA
            {
                get
                {
                    var sho = parent.m_Segments[2];
                    return new Vector4(sho.offsetX, sho.offsetY, sho.scaleX, sho.scaleY);
                }
            }

            /// <summary>
            /// A pre-built <see cref="Vector4"/> holding: <c>(sho.lnA, sho.B, 0, 0)</c>.
            /// </summary>
            public Vector4 shoSegmentB
            {
                get
                {
                    var sho = parent.m_Segments[2];
                    return new Vector4(sho.lnA, sho.B, 0f, 0f);
                }
            }
        }

        /// <summary>
        /// The builtin <see cref="Uniforms"/> instance for this curve.
        /// </summary>
        public readonly Uniforms uniforms;
    }
}
