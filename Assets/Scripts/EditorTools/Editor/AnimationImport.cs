using UnityEditor;
using System.Collections.Generic;

public class AnimationImport : AssetPostprocessor
{
    // TODO: Setup with importer version, so increments will reimport all animations

    void OnPreprocessAnimation()
    {
        var modelImporter = assetImporter as ModelImporter;

        // TODO: Apply scriptable object rule (or something), rather than hard coded path
        if (!modelImporter.assetPath.StartsWith("Assets/Animation/"))
            return;

        
        // Our game has a lot of fast movement and for this humanoid will solve a bit better with more keyframes to work with.
        // So we over sample and rely on the on compression to take out unwanted keys 
        modelImporter.humanoidOversampling = ModelImporterHumanoidOversampling.X2;

        var defaultClipMap = new Dictionary<string, ModelImporterClipAnimation>();

        foreach (var clip in modelImporter.defaultClipAnimations)
            defaultClipMap.Add(clip.name, clip);

        // TODO: Ensure all FBX takes exist as Unity clips

        // Ensure all Clips the come from FBX Takes (name rule) inherit frame range from the Take
        var clips = modelImporter.clipAnimations;
        foreach (var clip in clips)
        {
            if (defaultClipMap.ContainsKey(clip.name))
            {
                clip.firstFrame = defaultClipMap[clip.name].firstFrame;
                clip.lastFrame = defaultClipMap[clip.name].lastFrame;
            }
        }

        modelImporter.clipAnimations = clips;
    }
}