namespace UnityEngine.Rendering.PostProcessing
{
    // Raw, mostly unoptimized implementation of Hable's artist-friendly tonemapping curve
    // http://filmicworlds.com/blog/filmic-tonemapping-with-piecewise-power-curves/
    public class HableCurve
    {
        public class Segment
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

        public float whitePoint { get; private set; }
        public float inverseWhitePoint { get; private set; }
        public float x0 { get; private set; }
        public float x1 { get; private set; }

        public readonly Segment[] segments = new Segment[3];

        public HableCurve()
        {
            for (int i = 0; i < 3; i++)
                segments[i] = new Segment();

            uniforms = new Uniforms(this);
        }

        public float Eval(float x)
        {
            float normX = x * inverseWhitePoint;
            int index = (normX < x0) ? 0 : ((normX < x1) ? 1 : 2);
            var segment = segments[index];
            float ret = segment.Eval(normX);
            return ret;
        }

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

                var midSegment = segments[1];
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
                var toeSegment = segments[0];
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
                var shoulderSegment = segments[2];

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
                float scale = segments[2].Eval(1f);
                float invScale = 1f / scale;

                segments[0].offsetY *= invScale;
                segments[0].scaleY *= invScale;

                segments[1].offsetY *= invScale;
                segments[1].scaleY *= invScale;

                segments[2].offsetY *= invScale;
                segments[2].scaleY *= invScale;
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

        //
        // Uniform building for ease of use
        //
        public class Uniforms
        {
            HableCurve parent;

            internal Uniforms(HableCurve parent)
            {
                this.parent = parent;
            }

            public Vector4 curve
            {
                get { return new Vector4(parent.inverseWhitePoint, parent.x0, parent.x1, 0f); }
            }

            public Vector4 toeSegmentA
            {
                get
                {
                    var toe = parent.segments[0];
                    return new Vector4(toe.offsetX, toe.offsetY, toe.scaleX, toe.scaleY);
                }
            }

            public Vector4 toeSegmentB
            {
                get
                {
                    var toe = parent.segments[0];
                    return new Vector4(toe.lnA, toe.B, 0f, 0f);
                }
            }

            public Vector4 midSegmentA
            {
                get
                {
                    var mid = parent.segments[1];
                    return new Vector4(mid.offsetX, mid.offsetY, mid.scaleX, mid.scaleY);
                }
            }

            public Vector4 midSegmentB
            {
                get
                {
                    var mid = parent.segments[1];
                    return new Vector4(mid.lnA, mid.B, 0f, 0f);
                }
            }

            public Vector4 shoSegmentA
            {
                get
                {
                    var sho = parent.segments[2];
                    return new Vector4(sho.offsetX, sho.offsetY, sho.scaleX, sho.scaleY);
                }
            }

            public Vector4 shoSegmentB
            {
                get
                {
                    var sho = parent.segments[2];
                    return new Vector4(sho.lnA, sho.B, 0f, 0f);
                }
            }
        }

        public readonly Uniforms uniforms;
    }
}
