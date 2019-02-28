using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

class PostProcessingTests
{
    [Test]
    public void Profile_AddSettings()
    {
        var profile = NewProfile();

        var bloom = profile.AddSettings<Bloom>();
        Assert.IsNotNull(bloom);

        Destroy(profile);
    }

    [Test]
    public void Profile_HasSettings()
    {
        var profile = NewProfile(typeof(Bloom));

        Assert.IsTrue(profile.HasSettings<Bloom>());
        Assert.IsFalse(profile.HasSettings<ChromaticAberration>());

        Destroy(profile);
    }

    [Test]
    public void Profile_GetSettings()
    {
        var profile = NewProfile(typeof(Bloom));

        Assert.IsNotNull(profile.GetSetting<Bloom>());
        Assert.IsNull(profile.GetSetting<ChromaticAberration>());

        Destroy(profile);
    }

    [Test]
    public void Profile_TryGetSettings()
    {
        var profile = NewProfile(typeof(Bloom));

        Bloom outBloom;
        var exists = profile.TryGetSettings(out outBloom);
        Assert.IsTrue(exists);
        Assert.IsNotNull(outBloom);

        ChromaticAberration outChroma;
        exists = profile.TryGetSettings(out outChroma);
        Assert.IsFalse(exists);
        Assert.IsNull(outChroma);

        Destroy(profile);
    }

    [Test]
    public void Profile_RemoveSettings()
    {
        var profile = NewProfile(typeof(Bloom));

        profile.RemoveSettings<Bloom>();
        Assert.IsFalse(profile.HasSettings<Bloom>());

        Destroy(profile);
    }

    static PostProcessProfile NewProfile(params Type[] types)
    {
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();

        foreach (var t in types)
            profile.AddSettings(t);

        return profile;
    }

    static void Destroy(PostProcessProfile profile)
    {
        UnityEngine.Object.DestroyImmediate(profile);
    }
}
