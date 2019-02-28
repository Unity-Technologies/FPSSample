using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

abstract public class OptionUI : MonoBehaviour
{
    abstract public void UpdateFromConfigVar();
    abstract public void UpdateToConfigVar();

    [System.NonSerialized]
    public ConfigVar configVar;

    [System.NonSerialized]
    public bool changed;
    public void OnChanged()
    {
        changed = true;
    }
}

public class OptionsMenu : MonoBehaviour
{
    public OptionToggle toggleTemplate;
    public OptionDropdown dropdownTemplate;
    public OptionInput inputTemplate;
    public OptionHeading headingTemplate;
    public OptionSlider sliderTemplate;

    public Button gdrpButton;

    public ScrollRect scrollRect;
    public GameObject content;

    List<OptionUI> options = new List<OptionUI>();

    float height = 0.0f;

    void AddOption(OptionUI o)
    {
        options.Add(o);
        o.gameObject.SetActive(true);
        var rt = ((RectTransform)o.transform);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -height);
        height += rt.rect.height;

        // Resize scroll area
        var panelRect = ((RectTransform)transform).rect;
        var contentRect = ((RectTransform)content.transform);
        var cr = contentRect.sizeDelta;
        cr.y = height;
        contentRect.sizeDelta = cr;
    }

    void AddToggle(ConfigVar configVar, string description)
    {
        var input = Instantiate(toggleTemplate, content.transform);
        input.title.text = description;
        input.configVar = configVar;
        AddOption(input);
    }

    void AddInput(ConfigVar configVar, string description)
    {
        var input = Instantiate(inputTemplate, content.transform);
        input.title.text = description;
        input.configVar = configVar;
        AddOption(input);
    }

    void AddDropdown(ConfigVar configVar, string description, List<string> options, List<string> values, string custom = "")
    {
        var drop = Instantiate(dropdownTemplate, content.transform);
        drop.dropdown.ClearOptions();
        drop.dropdown.AddOptions(options);
        if (custom != "")
            drop.dropdown.AddOptions(new List<string> { custom });
        drop.title.text = description;
        drop.values = values;
        drop.configVar = configVar;
        AddOption(drop);
    }

    void AddSpace(float space)
    {
        height += space;
    }

    void AddHeading(string text)
    {
        var heading = Instantiate(headingTemplate, content.transform);
        heading.title.text = text;
        heading.configVar = null;
        AddOption(heading);
    }

    void AddSlider(ConfigVar configVar, string description)
    {
        var slider = Instantiate(sliderTemplate, content.transform);
        slider.title.text = description;
        slider.configVar = configVar;
        slider.slider.minValue = 0;
        slider.slider.maxValue = 1;
        slider.slider.wholeNumbers = false;
        AddOption(slider);
    }

    void Start()
    {
        var resHash = new HashSet<string>();
        foreach (var r in Screen.resolutions)
            resHash.Add(r.width + "x" + r.height/* + "@" + r.refreshRate*/);
        var res = new List<string>(resHash);
        res.Sort((a,b) =>
        {
            var asplit = a.Split('x');
            var bsplit = b.Split('x');
            return int.Parse(asplit[0]).CompareTo(int.Parse(bsplit[0])) * 10 + int.Parse(asplit[1]).CompareTo(int.Parse(bsplit[1]));
        });

        AddHeading("Graphics settings");
        AddDropdown(RenderSettings.rQuality, "Overall quality", new List<string>(QualitySettings.names), new List<string>(QualitySettings.names));
        AddDropdown(RenderSettings.rResolution, "Screen resolution", res, res, "Custom");
        AddDropdown(RenderSettings.rFullscreen, "Full screen mode", new List<string>() { "Windowed", "Full screen", "Exclusive" }, new List<string>() { "3", "1", "0" });
        AddToggle(RenderSettings.rVSync, "Enable v-sync");
        AddToggle(RenderSettings.rBloom, "Bloom effect");
        AddToggle(RenderSettings.rMotionBlur, "Motion blur effect");
        AddToggle(RenderSettings.rSSAO, "Screen space ambient occlusion");
        AddToggle(RenderSettings.rGrain, "Grain effect");
        AddToggle(RenderSettings.rSSR, "Screen space reflection");
        AddToggle(RenderSettings.rSSS, "Subsurface scattering");
        AddDropdown(RenderSettings.rAAMode, "Anti alias mode", new List<string>() { "Off", "FXAA", "SMAA", "TAA" }, new List<string>() { "off", "fxaa", "smaa", "taa" });

        AddSpace(100.0f);
        AddHeading("Audio settings");
        AddDropdown(SoundSystem.soundMute, "Audio enabled", new List<string>() { "When focus", "Always", "Never" }, new List<string>() { "-1", "1", "0" });
        AddDropdown(SoundSystem.soundSpatialize, "Output type", new List<string>() { "Speakers", "Headphones" }, new List<string>() { "0", "1" });
        AddSlider(SoundSystem.soundMusicVol, "Music volume");
        AddSlider(SoundSystem.soundSFXVol, "SFX volume");

        Canvas.ForceUpdateCanvases(); // TODO (petera) why is this needed?

        scrollRect.verticalNormalizedPosition = 1f;

#if ENABLE_CLOUD_SERVICES_ANALYTICS
        gdrpButton.gameObject.SetActive(true);
#else
        gdrpButton.gameObject.SetActive(false);
#endif
    }

    public void UpdateMenu()
    {
        foreach(var o in options)
        {
            if(o.changed)
            {
                o.changed = false;
                o.UpdateToConfigVar();
                Console.EnqueueCommandNoHistory("saveconfig");
            }
            else
                o.UpdateFromConfigVar();
        }
    }

    public void OnGDRP()
    {
#if ENABLE_CLOUD_SERVICES_ANALYTICS
        UnityEngine.Analytics.DataPrivacy.FetchPrivacyUrl(OnUrlReceived, OnFailure);
#endif
    }

#if ENABLE_CLOUD_SERVICES_ANALYTICS
    private void OnFailure(string err)
    {
        GameDebug.LogWarning("Failed to get Data Privacy URL: " + err);
    }

    private void OnUrlReceived(string url)
    {
        GameDebug.Log("Opening Privacy url: " + url);
        Application.OpenURL(url);
    }
#endif

}
