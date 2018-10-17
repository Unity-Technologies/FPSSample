using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class LightUtils
    {
        // Physical light unit helper
        // All light unit are in lumen (Luminous power)
        // Punctual light (point, spot) are convert to candela (cd = lumens / steradian)

        // For our isotropic area lights which expect radiance(W / (sr* m^2)) in the shader:
        // power = Integral{area, Integral{hemisphere, radiance * <N, L>}},
        // power = area * Pi * radiance,
        // radiance = power / (area * Pi).
        // We use photometric unit, so radiance is luminance and power is luminous power

        // Ref: Moving Frostbite to PBR
        // Also good ref: https://www.radiance-online.org/community/workshops/2004-fribourg/presentations/Wandachowicz_paper.pdf

        // convert intensity (lumen) to candela
        public static float ConvertPointLightLumenToCandela(float intensity)
        {
            return intensity / (4.0f * Mathf.PI);
        }

        // convert intensity (candela) to lumen
        public static float ConvertPointLightCandelaToLumen(float intensity)
        {
            return intensity * (4.0f * Mathf.PI);
        }

        // angle is the full angle, not the half angle in radiant
        // convert intensity (lumen) to candela
        public static float ConvertSpotLightLumenToCandela(float intensity, float angle, bool exact)
        {
            return exact ? intensity / (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity / Mathf.PI;
        }

        public static float ConvertSpotLightCandelaToLumen(float intensity, float angle, bool exact)
        {
            return exact ? intensity * (2.0f * (1.0f - Mathf.Cos(angle / 2.0f)) * Mathf.PI) : intensity * Mathf.PI;
        }

        // angleA and angleB are the full opening angle, not half angle
        // convert intensity (lumen) to candela
        public static float ConvertFrustrumLightLumenToCandela(float intensity, float angleA, float angleB)
        {
            return intensity / (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));
        }

        public static float ConvertFrustrumLightCandelaToLumen(float intensity, float angleA, float angleB)
        {
            return intensity * (4.0f * Mathf.Asin(Mathf.Sin(angleA / 2.0f) * Mathf.Sin(angleB / 2.0f)));
        }

        // convert intensity (lumen) to nits
        public static float ConvertSphereLightLumenToLuminance(float intensity, float sphereRadius)
        {
            return intensity / ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);
        }

        // convert intensity (nits) to lumen
        public static float ConvertSphereLightLuminanceToLumen(float intensity, float sphereRadius)
        {
            return intensity * ((4.0f * Mathf.PI * sphereRadius * sphereRadius) * Mathf.PI);
        }

        // convert intensity (lumen) to nits
        public static float ConvertDiscLightLumenToLuminance(float intensity, float discRadius)
        {
            return intensity / ((discRadius * discRadius * Mathf.PI) * Mathf.PI);
        }

        // convert intensity (nits) to lumen
        public static float ConvertDiscLightLuminanceToLumen(float intensity, float discRadius)
        {
            return intensity * ((discRadius * discRadius * Mathf.PI) * Mathf.PI);
        }

        // convert intensity (lumen) to nits
        public static float ConvertRectLightLumenToLuminance(float intensity, float width, float height)
        {
            return intensity / ((width * height) * Mathf.PI);
        }

        // convert intensity (nits) to lumen
        public static float ConvertRectLightLuminanceToLumen(float intensity, float width, float height)
        {
            return intensity * ((width * height) * Mathf.PI);
        }

        public static float ConvertEvToLuminance(float ev)
        {
            return Mathf.Pow(2, ev - 3);
        }

        public static float ConvertLuminanceToEv(float luminance)
        {
            const float k = 12.5f;

            return (float)Math.Log((luminance * 100f) / k, 2);
        }

        public static float ConvertAreaLightLumenToLuminance(LightTypeExtent areaLightType, float lumen, float width, float height = 0)
        {
            switch (areaLightType)
            {
                case LightTypeExtent.Line:
                    return LightUtils.CalculateLineLightLumenToLuminance(lumen, width);
                case LightTypeExtent.Rectangle:
                    return LightUtils.ConvertRectLightLumenToLuminance(lumen, width, height);
            }
            return lumen;
        }

        public static float ConvertAreaLightLuminanceToLumen(LightTypeExtent areaLightType, float luminance, float width, float height = 0)
        {
            switch (areaLightType)
            {
                case LightTypeExtent.Line:
                    return LightUtils.CalculateLineLightLuminanceToLumen(luminance, width);
                case LightTypeExtent.Rectangle:
                    return LightUtils.ConvertRectLightLuminanceToLumen(luminance, width, height);
            }
            return luminance;
        }

        public static float ConvertAreaLightLumenToEv(LightTypeExtent areaLightType, float lumen, float width, float height)
        {
            float luminance = ConvertAreaLightLumenToLuminance(areaLightType, lumen, width, height);

            return ConvertLuminanceToEv(luminance);
        }

        public static float ConvertAreaLightEvToLumen(LightTypeExtent areaLightType, float ev, float width, float height)
        {
            float luminance = ConvertEvToLuminance(ev);

            return ConvertAreaLightLuminanceToLumen(areaLightType, luminance, width, height);
        }

        // convert intensity (lumen) to nits
        public static float CalculateLineLightLumenToLuminance(float intensity, float lineWidth)
        {
            //Line lights expect radiance (W / (sr * m^2)) in the shader.
            //In the UI, we specify luminous flux (power) in lumens.
            //First, it needs to be converted to radiometric units (radiant flux, W).

            //Then we must recall how to compute power from radiance:

            //radiance = differential_power / (differrential_projected_area * differential_solid_angle),
            //radiance = differential_power / (differrential_area * differential_solid_angle * <N, L>),
            //power = Integral{area, Integral{hemisphere, radiance * <N, L>}}.

            //Unlike tube lights, our line lights have no surface area, so the integral becomes:

            //power = Integral{length, Integral{sphere, radiance}}.

            //For an isotropic line light, radiance is constant, therefore:

            //power = length * (4 * Pi) * radiance,
            //radiance = power / (length * (4 * Pi)).
            return intensity / (4.0f * Mathf.PI * lineWidth);
        }

        public static float CalculateLineLightLuminanceToLumen(float intensity, float lineWidth)
        {
            return intensity * (4.0f * Mathf.PI * lineWidth);
        }

        // spotAngle in radiant
        public static void CalculateAnglesForPyramid(float aspectRatio, float spotAngle, out float angleA, out float angleB)
        {
            // Since the smallest angles is = to the fov, and we don't care of the angle order, simply make sure the aspect ratio is > 1
            if (aspectRatio < 1.0f)
                aspectRatio = 1.0f / aspectRatio;

            angleA = spotAngle;

            var halfAngle = angleA * 0.5f; // half of the smallest angle
            var length = Mathf.Tan(halfAngle); // half length of the smallest side of the rectangle
            length *= aspectRatio; // half length of the bigest side of the rectangle
            halfAngle = Mathf.Atan(length); // half of the bigest angle

            angleB = halfAngle * 2.0f;
        }

        // TODO: Do a cheaper fitting
        // Given a correlated color temperature (in Kelvin), estimate the RGB equivalent. Curve fit error is max 0.008.
        // return color in linear RGB space
        public static Color CorrelatedColorTemperatureToRGB(float temperature)
        {
            float r, g, b;

            // Temperature must fall between 1000 and 40000 degrees
            // The fitting require to divide kelvin by 1000 (allow more precision)
            float kelvin = Mathf.Clamp(temperature, 1000.0f, 40000.0f) / 1000.0f;
            float kelvin2 = kelvin * kelvin;

            // Using 6570 as a pivot is an approximation, pivot point for red is around 6580 and for blue and green around 6560.
            // Calculate each color in turn (Note, clamp is not really necessary as all value belongs to [0..1] but can help for extremum).
            // Red
            r = kelvin < 6.570f ? 1.0f : Mathf.Clamp((1.35651f + 0.216422f * kelvin + 0.000633715f * kelvin2) / (-3.24223f + 0.918711f * kelvin), 0.0f, 1.0f);
            // Green
            g = kelvin < 6.570f ?
                Mathf.Clamp((-399.809f + 414.271f * kelvin + 111.543f * kelvin2) / (2779.24f + 164.143f * kelvin + 84.7356f * kelvin2), 0.0f, 1.0f) :
                Mathf.Clamp((1370.38f + 734.616f * kelvin + 0.689955f * kelvin2) / (-4625.69f + 1699.87f * kelvin), 0.0f, 1.0f);
            //Blue
            b = kelvin > 6.570f ? 1.0f : Mathf.Clamp((348.963f - 523.53f * kelvin + 183.62f * kelvin2) / (2848.82f - 214.52f * kelvin + 78.8614f * kelvin2), 0.0f, 1.0f);

            return new Color(r, g, b, 1.0f);
        }
    }
}
