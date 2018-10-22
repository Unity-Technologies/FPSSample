using UnityEditor;
using UnityEngine;

class TextureSettingsTool
{
    /// <summary>
    /// These are the rules that are applied when a new texture is imported and on-demand by the 
    /// artists. The rules are verified when a build is made.
    /// </summary>
    static TextureSetting[] settings = new TextureSetting[]
    {
        new TextureSetting
        {
            magicString = "_Albedo.",
            anisoLevel = 4,
            textureType = TextureImporterType.Default,
            sRGBTexture = true,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC7,
                },
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC7
                }
            }
        },
        new TextureSetting
        {
            magicString = "_MaskMap.",
            anisoLevel = 4,
            textureType = TextureImporterType.Default,
            sRGBTexture = false,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC7
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC7
                }
            }
        },
        new TextureSetting
        {
            magicString = "_LayerMask.",
            anisoLevel = 4,
            textureType = TextureImporterType.Default,
            sRGBTexture = true,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC7
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC7
                }
            },
        },
        new TextureSetting
        {
            magicString = "_Normal.",
            anisoLevel = 4,
            textureType = TextureImporterType.NormalMap,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC5
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC5
                }
            },
        },
        new TextureSetting
        {
            magicString = "_BNM.",
            anisoLevel = 4,
            textureType = TextureImporterType.NormalMap,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Size = 256,
                    Format = TextureImporterFormat.BC5
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Size = 256,
                    Format = TextureImporterFormat.BC5
                }
            },
        },
        new TextureSetting
        {
            magicString = "_Height.",
            anisoLevel = 4,
            sRGBTexture = false,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            textureType = TextureImporterType.SingleChannel,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC4
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC4
                }
            },
        },
        new TextureSetting
        {
            magicString = "_Thickness.",
            anisoLevel = 4,
            sRGBTexture = false,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            textureType = TextureImporterType.SingleChannel,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC4
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC4
                }
            },
        },
        new TextureSetting
        {
            magicString = "_Detail.",
            anisoLevel = 4,
            sRGBTexture = false,
            streamingMipmaps = true,
            textureCompression = TextureImporterCompression.CompressedHQ,
            textureType = TextureImporterType.Default,
            platformSettings = new PlatformSettings[]
            {
                new PlatformSettings
                {
                    Name = "Standalone",
                    Format = TextureImporterFormat.BC7
                },
                new PlatformSettings
                {
                    Name = "PS4",
                    Format = TextureImporterFormat.BC7
                }
            },
        }
    };
    class PlatformSettings
    {
        public string Name;
        public int Size;
        public TextureImporterFormat Format;

        public void ApplyPlatformSettings(TextureImporter importer)
        {
            var platformSettings = new TextureImporterPlatformSettings();
            importer.GetDefaultPlatformTextureSettings().CopyTo(platformSettings);
            platformSettings.overridden = true;
            platformSettings.name = Name;
            if (Size > 0)
            {
                platformSettings.maxTextureSize = Size;
            }
            platformSettings.format = Format;
            importer.SetPlatformTextureSettings(platformSettings);
        }
    }

    class TextureSetting
    {
        public string magicString;
        public int anisoLevel;
        public TextureImporterType textureType;
        public bool sRGBTexture;
        public bool streamingMipmaps;
        public TextureImporterCompression textureCompression;
        public PlatformSettings[] platformSettings;

        public bool ShouldApply(string assetPath)
        {
            return assetPath.Contains(magicString);
        }

        public bool VerifyLog(TextureImporter importer)
        {
            var error = Verify(importer);
            bool ok = error == "";
            if (ok)
                Debug.Log("Checking " + importer.assetPath + ": <color=#00ff00>Ok</color>");
            else
                Debug.LogWarning("Checking " + importer.assetPath + ": <color=#ff0000>Failed</color> (" + error + ")", AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(Texture)));
            return ok;
        }

        public string Verify(TextureImporter importer)
        {
            if (importer.anisoLevel != anisoLevel) return string.Format("anisoLevel should be {0} but is {1}", anisoLevel, importer.anisoLevel);
            if (importer.textureType != textureType) return string.Format("textureType should be {0} but is {1}", textureType, importer.textureType);
            if (importer.sRGBTexture != sRGBTexture) return string.Format("sRGBTexture should be {0} but is {1}", sRGBTexture, importer.sRGBTexture);
            if (importer.streamingMipmaps != streamingMipmaps) return string.Format("streamingMipmaps should be {0} but is {1}", streamingMipmaps, importer.streamingMipmaps);
            if (importer.textureCompression != textureCompression) return string.Format("textureCompression should be {0} but is {1}", textureCompression, importer.textureCompression);
            foreach (var s in platformSettings)
            {
                var ps = importer.GetPlatformTextureSettings(s.Name);
                if (ps.overridden != true) return "no override for " + s.Name;
                if (ps.format != s.Format) return string.Format("format for {0} should be {1} but is {2}", s.Name, s.Format, ps.format);
                if (s.Size > 0 && ps.maxTextureSize != s.Size) string.Format("maxsize for {0} should be {1} but is {2}", s.Name, s.Size, ps.maxTextureSize);
            }
            return "";
        }

        public void Apply(TextureImporter importer)
        {
            importer.anisoLevel = anisoLevel;
            importer.textureType = textureType;
            importer.sRGBTexture = sRGBTexture;
            importer.streamingMipmaps = streamingMipmaps;
            importer.textureCompression = textureCompression;
            foreach (var s in platformSettings)
            {
                s.ApplyPlatformSettings(importer);
            }
        }
    }


    [MenuItem("Assets/TextureRules/ApplyTextureSettings")]
    public static void ApplyTextureSettings()
    {
        foreach(var g in Selection.assetGUIDs)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            ApplyRules(importer);
            AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
        }
    }

    [MenuItem("Assets/TextureRules/VerifyTextureSettings")]
    public static void VerifySelectedTextureSettings()
    {
        VerifyTextureSettings(Selection.assetGUIDs);
    }

    [MenuItem("FPS Sample/VerifyTextureSettings")]
    public static void VerifyTextureSettings()
    {
        var textures = AssetDatabase.FindAssets("t:texture");
        VerifyTextureSettings(textures);
    }

    public static void VerifyTextureSettings(string[] textures)
    {

        Debug.Log("Verifying texture import settings");
        Debug.Log("=================================");
        int noImporter = 0;
        int noRule = 0;
        int verifiedOk = 0;
        int verifiedNotOk = 0;
        foreach (var t in textures)
        {
            var path = AssetDatabase.GUIDToAssetPath(t);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                noImporter++;
                Debug.Log("Texture " + path + " has no importer?");
                continue;
            }
            bool verified = false;
            foreach (var s in settings)
            {
                if (s.ShouldApply(path))
                {
                    verified = true;
                    bool ok = s.VerifyLog(importer);
                    if (ok) verifiedOk++;
                    else verifiedNotOk++;
                    break;
                }
            }
            if (!verified)
            {
                //Debug.Log("Texture " + path + " matches no rule");
                noRule++;
            }
        }
        Debug.Log("===================");
        Debug.Log("Num textures: " + textures.Length);
        Debug.Log("Num skipped : " + noRule);
        Debug.Log("Num failed  : " + verifiedNotOk);
        Debug.Log("Num ok      : " + verifiedOk);
        Debug.Log("===================");
    }

    public static void ApplyRules(TextureImporter importer)
    {
        foreach (var s in settings)
        {
            if (s.ShouldApply(importer.assetPath))
            {
                Debug.Log("Applying " + s.magicString + " rules to " + importer.assetPath);
                s.Apply(importer);
                break;
            }
        }
    }
}

class TexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        var textureImporter = assetImporter as TextureImporter;

        // Skip bad
        if (!textureImporter)
            return;

        // Only apply if initial import
        var existing = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture));
        if (existing != null)
            return;

        TextureSettingsTool.ApplyRules(textureImporter);
    }
}