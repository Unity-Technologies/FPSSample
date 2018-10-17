using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // TODO: Missing UI tooltips
    partial class HDLightEditor
    {
        sealed class Styles
        {
            // Base (copy from LightEditor.cs)
            public readonly GUIContent cookieSizeX = new GUIContent("Size X", "Controls the size of the cookie mask currently assigned to the light.");
            public readonly GUIContent cookieSizeY = new GUIContent("Size Y", "Controls the size of the cookie mask currently assigned to the light.");
            public readonly GUIContent shadowBias = new GUIContent("Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNormalBias = new GUIContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNearPlane = new GUIContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            public readonly GUIContent bakedShadowRadius = new GUIContent("Baked Shadow Radius", "Controls the amount of artificial softening applied to the edges of shadows cast by the Point or Spot light.");
            public readonly GUIContent bakedShadowAngle = new GUIContent("Baked Shadow Angle", "Controls the amount of artificial softening applied to the edges of shadows cast by directional lights.");
            public readonly GUIContent lightBounceIntensity = new GUIContent("Indirect Multiplier", "Controls the intensity of indirect light being contributed to the scene. A value of 0 will cause Realtime lights to be removed from realtime global illumination and Baked and Mixed lights to no longer emit indirect lighting. Has no effect when both Realtime and Baked Global Illumination are disabled.");
            public readonly GUIContent indirectBounceShadowWarning = new GUIContent("Realtime indirect bounce shadowing is not supported for Spot and Point lights.");

            // Additional light data
            public readonly GUIContent directionalIntensity = new GUIContent("Intensity (Lux)", "Illuminance of the directional light at ground level in lux.");
            public readonly GUIContent punctualIntensity = new GUIContent("Intensity (Lumen)", "Luminous power of the light in lumen. Spotlight are considered as point light with barndoor so match intensity of a point light.");
            public readonly GUIContent areaIntensity = new GUIContent("Intensity (Lumen)", "Luminous power of the light in lumen.");
            public readonly GUIContent lightIntensity = new GUIContent("Intensity", "");

            public readonly GUIContent maxSmoothness = new GUIContent("Max Smoothness", "Very low cost way of faking spherical area lighting. This will modify the roughness of the material lit. This is useful when the specular highlight is too small or too sharp.");
            public readonly GUIContent lightRadius = new GUIContent("Light Radius", "Can be used to soften the core of the punctual light to create fill lighting.");
            public readonly GUIContent affectDiffuse = new GUIContent("Affect Diffuse", "This will disable diffuse lighting for this light. Doesn't save performance, diffuse lighting is still computed.");
            public readonly GUIContent affectSpecular = new GUIContent("Affect Specular", "This will disable specular lighting for this light. Doesn't save performance, specular lighting is still computed.");
            public readonly GUIContent nonLightmappedOnly = new GUIContent("Non Lightmapped Only", "This will disable the rendering of lightmapped object for shadow map. Only effective with shadow mask feature");
            public readonly GUIContent lightDimmer = new GUIContent("Dimmer", "Aim to be used with script, timeline or animation. It allows dimming one or multiple lights of heterogeneous intensity easily (without needing to know the intensity of each light).");
            public readonly GUIContent volumetricDimmer = new GUIContent("Volumetric Dimmer", "Allows to reduce the intensity of the scattered volumetric lighting.");
            public readonly GUIContent fadeDistance = new GUIContent("Fade Distance", "The distance at which the light will smoothly fade before being culled to minimize popping.");
            public readonly GUIContent spotInnerPercent = new GUIContent("Inner Percent", "Controls size of the angular attenuation in percent of the base angle of the Spot light's cone.");
            public readonly GUIContent spotLightShape = new GUIContent("Shape", "The shape use for the spotlight. Has an impact on the cookie transformation and light angular attenuation.");
            public readonly GUIContent shapeWidthLine = new GUIContent("Length", "Length of the line light");
            public readonly GUIContent shapeWidthRect = new GUIContent("Size X", "SizeX of the rectangle light");
            public readonly GUIContent shapeHeightRect = new GUIContent("Size Y", "SizeY of the rectangle light");
            public readonly GUIContent aspectRatioPyramid = new GUIContent("Aspect ratio", "");
            public readonly GUIContent shapeWidthBox = new GUIContent("Size X", "");
            public readonly GUIContent shapeHeightBox = new GUIContent("Size Y", "");
            public readonly GUIContent applyRangeAttenuation = new GUIContent("Apply Range Attenuation", "Allows disabling range attenuation. This is useful indoor (like a room) to avoid having to setup a large range for a light to get correct inverse square attenuation that may leak out of the indoor");
            public readonly GUIContent displayAreaLightEmissiveMesh = new GUIContent("Display Emissive Mesh", "Generate an emissive mesh using the size, color and intensity of the area light");
            public readonly GUIContent lightLayer = new GUIContent("Light Layer", "Specifies the current light layers that the light affect. Corresponding renderer with the same flags will be lit by this light.");

            public readonly GUIContent sunDiskSize = new GUIContent("Sun Highlight Disk Size", "Controls the size of the highlight of the sun disk. It's the angle of the sun cone in degrees.");
            public readonly GUIContent sunHaloSize = new GUIContent("Sun Highlight Halo Size", "Controls the size of the halo around the highlight of the sun disk.");

            public readonly GUIContent shape = new GUIContent("Type", "Specifies the current type of light. Possible types are Directional, Spot, Point, Rectangle and Line lights.");
            public readonly GUIContent[] shapeNames;
            public readonly GUIContent enableSpotReflector = new GUIContent("Angle affect intensity", "When true it simulate a spot light with reflector (mean the intensity of the light will be more focus with narrower angle), otherwise light outside of the cone is simply absorbed (mean intensity is constent whatever the size of the cone).");

            // Additional shadow data
            public readonly GUIContent shadowResolution = new GUIContent("Resolution", "Controls the rendered resolution of the shadow maps. A higher resolution will increase the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent shadowFadeDistance = new GUIContent("Fade Distance", "The shadow will fade at distance ShadowFadeDistance before being culled to minimize popping.");
            public readonly GUIContent shadowDimmer = new GUIContent("Dimmer", "Aim to be use with script, timeline or animation. It allows dimming one or multiple shadows. This can also be used as an optimization to fit in shadow budget manually and minimize popping.");
            public readonly GUIContent volumetricShadowDimmer = new GUIContent("Volumetric Dimmer", "Aim to be use with script, timeline or animation. It allows dimming one or multiple shadows. This can also be used as an optimization to fit in shadow budget manually and minimize popping.");
            public readonly GUIContent contactShadows = new GUIContent("Enable Contact Shadows", "Enable support for contact shadows on this light. Better for lights with a lot of visible shadows.");

            // Bias control
            public readonly GUIContent viewBiasMin = new GUIContent("View Bias");
            public readonly GUIContent viewBiasMax = new GUIContent("View Bias Max");
            public readonly GUIContent viewBiasScale = new GUIContent("View Bias Scale");
            public readonly GUIContent normalBiasMin = new GUIContent("Normal Bias");
            public readonly GUIContent normalBiasMax = new GUIContent("Normal Bias Max");
            public readonly GUIContent normalBiasScale = new GUIContent("Normal Bias Scale");
            public readonly GUIContent sampleBiasScale = new GUIContent("Sample Bias Scale");
            public readonly GUIContent edgeLeakFixup = new GUIContent("Edge Leak Fixup");
            public readonly GUIContent edgeToleranceNormal = new GUIContent("Edge Tolerance Normal");
            public readonly GUIContent edgeTolerance = new GUIContent("Edge Tolerance");

            // Shadow filter settings
            public readonly GUIContent shadowSoftness = new GUIContent("Shadow Softness", "Size of the penumbra");
            public readonly GUIContent blockerSampleCount = new GUIContent("Blocker Sample Count", "Sample count used to determine the size of the blocker");
            public readonly GUIContent filterSampleCount = new GUIContent("Filter Sample Count");

            public Styles()
            {
                shapeNames = Enum.GetNames(typeof(LightShape))
                    .Select(x => new GUIContent(x))
                    .ToArray();
            }
        }

        static Styles s_Styles;

        // Can't use a static initializer in case we need to create GUIStyle in the Styles class as
        // these can only be created with an active GUI rendering context
        void CheckStyles()
        {
            if (s_Styles == null)
                s_Styles = new Styles();
        }
    }
}
