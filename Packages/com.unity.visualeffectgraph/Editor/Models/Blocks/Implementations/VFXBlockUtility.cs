using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    enum AttributeCompositionMode
    {
        Overwrite,
        Add,
        Multiply,
        Blend
    }

    enum TextureDataEncoding
    {
        UnsignedNormalized,
        Signed
    }

    enum RandomMode
    {
        Off,
        PerComponent,
        Uniform,
    }

    class VFXBlockUtility
    {
        public static string GetNameString(AttributeCompositionMode mode)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return "Set";
                case AttributeCompositionMode.Add: return "Add";
                case AttributeCompositionMode.Multiply: return "Multiply";
                case AttributeCompositionMode.Blend: return "Blend";
                default: throw new ArgumentException();
            }
        }

        public static string GetNameString(RandomMode mode)
        {
            switch (mode)
            {
                case RandomMode.Off: return "";
                case RandomMode.PerComponent: return "Random";
                case RandomMode.Uniform: return "Random";
                default: throw new ArgumentException();
            }
        }

        public static string GetComposeString(AttributeCompositionMode mode, params string[] parameters)
        {
            switch (mode)
            {
                case AttributeCompositionMode.Overwrite: return string.Format("{0} = {1};", parameters);
                case AttributeCompositionMode.Add: return string.Format("{0} += {1};", parameters);
                case AttributeCompositionMode.Multiply: return string.Format("{0} *= {1};", parameters);
                case AttributeCompositionMode.Blend: return string.Format("{0} = lerp({0},{1},{2});", parameters);
                default: throw new System.NotImplementedException("VFXBlockUtility.GetComposeFormatString() does not implement return string for : " + mode.ToString());
            }
        }

        public static string GetRandomMacroString(RandomMode mode, int attributeSize, string postfix, params string[] parameters)
        {
            switch (mode)
            {
                case RandomMode.Off:
                    return parameters[0] + postfix;
                case RandomMode.Uniform:
                    return string.Format("lerp({0},{1},RAND)", parameters.Select(s => s + postfix).ToArray());
                case RandomMode.PerComponent:
                    string rand = GetRandStringFromSize(attributeSize);
                    return string.Format("lerp({0},{1}," + rand + ")", parameters.Select(s => s + postfix).ToArray());
                default: throw new System.NotImplementedException("VFXBlockUtility.GetRandomMacroString() does not implement return string for RandomMode : " + mode.ToString());
            }
        }

        public static string GetRandStringFromSize(int size)
        {
            if (size < 0 || size > 4)
                throw new ArgumentOutOfRangeException("Size can be only of 1, 2, 3 or 4");

            return "RAND" + ((size != 1) ? size.ToString() : "");
        }

        // TODO Remove that
        public static string GetSizeVector(VFXContext context, int nbComponents = 3)
        {
            var data = context.GetData();

            string scaleX = data.IsCurrentAttributeRead(VFXAttribute.ScaleX, context) ? "scaleX" : "1.0f";
            string scaleY = nbComponents >= 2 && data.IsCurrentAttributeRead(VFXAttribute.ScaleY, context) ? "scaleY" : "1.0f";
            string scaleZ = nbComponents >= 3 && data.IsCurrentAttributeRead(VFXAttribute.ScaleZ, context) ? "scaleZ" : "1.0f";

            switch (nbComponents)
            {
                case 1: return string.Format("(size * {0})", scaleX);
                case 2: return string.Format("(size * float2({0},{1}))", scaleX, scaleY);
                case 3: return string.Format("(size * float3({0},{1},{2}))", scaleX, scaleY, scaleZ);
                default:
                    throw new ArgumentException("NbComponents must be between 1 and 3");
            }
        }

        // TODO Remove that
        public static string SetSizesFromVector(VFXContext context, string vector, int nbComponents = 3)
        {
            if (nbComponents < 1 || nbComponents > 3)
                throw new ArgumentException("NbComponents must be between 1 and 3");

            var data = context.GetData();

            string res = string.Empty;

            if (data.IsCurrentAttributeWritten(VFXAttribute.ScaleX, context))
                res += string.Format("scaleX = {0}.x / size;\n", vector);
            if (nbComponents >= 2 && data.IsCurrentAttributeWritten(VFXAttribute.ScaleY, context))
                res += string.Format("scaleY = {0}.y / size;\n", vector);
            if (nbComponents >= 3 && data.IsCurrentAttributeWritten(VFXAttribute.ScaleZ, context))
                res += string.Format("scaleZ = {0}.z / size;\n", vector);

            return res.TrimEnd(new[] { '\n' });
        }

        public static bool ConvertToVariadicAttributeIfNeeded(ref string attribName, out VariadicChannelOptions outChannel)
        {
            var attrib = VFXAttribute.Find(attribName);

            if (attrib.variadic == VFXVariadic.BelongsToVariadic)
            {
                char component = attrib.name.ToLower().Last();
                VariadicChannelOptions channel;
                switch (component)
                {
                    case 'x':
                        channel = VariadicChannelOptions.X;
                        break;
                    case 'y':
                        channel = VariadicChannelOptions.Y;
                        break;
                    case 'z':
                        channel = VariadicChannelOptions.Z;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Cannot convert {0} to variadic version", attrib.name));
                }

                attribName = VFXAttribute.Find(attrib.name.Substring(0, attrib.name.Length - 1)).name; // Just to ensure the attribute can be found
                outChannel = channel;

                return true;
            }

            outChannel = VariadicChannelOptions.X;
            return false;
        }

        static VariadicChannelOptions ChannelFromMask(string mask)
        {
            mask = mask.ToLower();
            if (mask == "x")
                return VariadicChannelOptions.X;
            else if (mask == "y")
                return VariadicChannelOptions.Y;
            else if (mask == "z")
                return VariadicChannelOptions.Z;
            else if (mask == "xy")
                return VariadicChannelOptions.XY;
            else if (mask == "xz")
                return VariadicChannelOptions.XZ;
            else if (mask == "yz")
                return VariadicChannelOptions.YZ;
            else if (mask == "xyz")
                return VariadicChannelOptions.XYZ;
            return VariadicChannelOptions.X;
        }

        static string MaskFromChannel(VariadicChannelOptions channel)
        {
            switch (channel)
            {
                case VariadicChannelOptions.X: return "x";
                case VariadicChannelOptions.Y: return "y";
                case VariadicChannelOptions.Z: return "z";
                case VariadicChannelOptions.XY: return "xy";
                case VariadicChannelOptions.XZ: return "xz";
                case VariadicChannelOptions.YZ: return "yz";
                case VariadicChannelOptions.XYZ: return "xyz";
            }
            throw new InvalidOperationException("MaskFromChannel missing for " + channel);
        }

        public static bool ConvertSizeAttributeIfNeeded(ref string attribName, ref VariadicChannelOptions channels)
        {
            if (attribName == "size")
            {
                if (channels == VariadicChannelOptions.X) // Consider sizeX as uniform
                {
                    return true;
                }
                else
                {
                    attribName = "scale";
                    return true;
                }       
            }

            if (attribName == "sizeX")
            {
                attribName = "size";
                channels = VariadicChannelOptions.X;
                return true;
            }

            if (attribName == "sizeY")
            {
                attribName = "scale";
                channels = VariadicChannelOptions.Y;
                return true;
            }

            if (attribName == "sizeZ")
            {
                attribName = "scale";
                channels = VariadicChannelOptions.Z;
                return true;
            }

            return false;
        }

        public static bool SanitizeAttribute(ref string attribName, ref VariadicChannelOptions channels, int version)
        {
            bool settingsChanged = false;
            string oldName = attribName;
            VariadicChannelOptions oldChannels = channels;

            if (version < 1 && channels == VariadicChannelOptions.XZ) // Enumerators have changed
            {
                channels = VariadicChannelOptions.XYZ;
                settingsChanged = true;
            }

            if (version < 1 && VFXBlockUtility.ConvertSizeAttributeIfNeeded(ref attribName, ref channels))
            {
                Debug.Log(string.Format("Sanitizing attribute: Convert {0} with channel {2} to {1}", oldName, attribName, oldChannels));
                settingsChanged = true;
            }

            // Changes attribute to variadic version
            VariadicChannelOptions newChannels;
            if (VFXBlockUtility.ConvertToVariadicAttributeIfNeeded(ref attribName, out newChannels))
            {
                Debug.Log(string.Format("Sanitizing attribute: Convert {0} to variadic attribute {1} with channel {2}", oldName, attribName, newChannels));
                channels = newChannels;
                settingsChanged = true;
            }

            return settingsChanged;
        }

        static public bool SanitizeAttribute(ref string attribName, ref string channelsMask, int version)
        {
            var channels = ChannelFromMask(channelsMask);
            var settingsChanged = SanitizeAttribute(ref attribName, ref channels, version);
            channelsMask = MaskFromChannel(channels);
            return settingsChanged;
        }
    }
}
