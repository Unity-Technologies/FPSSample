// This is implementation of parallax occlusion mapping (POM)
// This function require that the caller define a callback for the height sampling name ComputePerPixelHeightDisplacement
// A PerPixelHeightDisplacementParam is used to provide all data necessary to calculate the heights to ComputePerPixelHeightDisplacement it doesn't need to be
// visible by the POM algorithm.
// This function is compatible with tiled uv.
// it return the offset to apply to the UVSet provide in PerPixelHeightDisplacementParam
// viewDirTS is view vector in texture space matching the UVSet
// ref: https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
real2 ParallaxOcclusionMapping(real lod, real lodThreshold, int numSteps, real3 viewDirTS, PerPixelHeightDisplacementParam ppdParam, out real outHeight)
{
    // Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
    real stepSize = 1.0 / (real)numSteps;

    // View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
    // The length of viewDirTS vector determines the furthest amount of displacement:
    // real parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
    // real2 parallaxDir = normalize(Out.viewDirTS.xy);
    // real2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
    // Above code simplify to
    real2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);
    real2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;

    // Do a first step before the loop to init all value correctly
    real2 texOffsetCurrent = real2(0.0, 0.0);
    real prevHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
    texOffsetCurrent += texOffsetPerStep;
    real currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
    real rayHeight = 1.0 - stepSize; // Start at top less one sample

    // Linear search
    for (int stepIndex = 0; stepIndex < numSteps; ++stepIndex)
    {
        // Have we found a height below our ray height ? then we have an intersection
        if (currHeight > rayHeight)
            break; // end the loop

        prevHeight = currHeight;
        rayHeight -= stepSize;
        texOffsetCurrent += texOffsetPerStep;

        // Sample height map which in this case is stored in the alpha channel of the normal map:
        currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
    }

    // Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation

    // Refine the search with secant method
#define POM_SECANT_METHOD 1
#if POM_SECANT_METHOD

    real pt0 = rayHeight + stepSize;
    real pt1 = rayHeight;
    real delta0 = pt0 - prevHeight;
    real delta1 = pt1 - currHeight;

    real delta;
    real2 offset;

    // Secant method to affine the search
    // Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
    for (int i = 0; i < 3; ++i)
    {
        // intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
        real intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
        // Retrieve offset require to find this intersectionHeight
        offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;

        currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);

        delta = intersectionHeight - currHeight;

        if (abs(delta) <= 0.01)
            break;

        // intersectionHeight < currHeight => new lower bounds
        if (delta < 0.0)
        {
            delta1 = delta;
            pt1 = intersectionHeight;
        }
        else
        {
            delta0 = delta;
            pt0 = intersectionHeight;
        }
    }

#else // regular POM intersection

    //real pt0 = rayHeight + stepSize;
    //real pt1 = rayHeight;
    //real delta0 = pt0 - prevHeight;
    //real delta1 = pt1 - currHeight;
    //real intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
    //real2 offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;

    // A bit more optimize
    real delta0 = currHeight - rayHeight;
    real delta1 = (rayHeight + stepSize) - prevHeight;
    real ratio = delta0 / (delta0 + delta1);
    real2 offset = texOffsetCurrent - ratio * texOffsetPerStep;

    currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);

#endif

    outHeight = currHeight;

    // Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
    offset *= (1.0 - saturate(lod - lodThreshold));

    return offset;
}
