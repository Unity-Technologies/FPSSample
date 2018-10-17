using System;
using UnityEngine;

public struct FormatSpec
{
    public int argWidth;
    public bool leadingZero;
    public int numberWidth;
    public int fractWidth;
}

public interface IConverter<T>
{
    unsafe void Convert(ref char* dst, char* end, T value, FormatSpec formatSpec);
}

public class Converter : IConverter<int>, IConverter<float>, IConverter<string>, IConverter<byte>, IConverter<CharBufView>
{
    public static Converter instance = new Converter();

    unsafe void IConverter<int>.Convert(ref char* dst, char* end, int value, FormatSpec formatSpec)
    {
        ConvertInt(ref dst, end, value, formatSpec.argWidth, formatSpec.numberWidth, formatSpec.leadingZero);
    }

    unsafe void IConverter<byte>.Convert(ref char* dst, char* end, byte value, FormatSpec formatSpec)
    {
        ConvertInt(ref dst, end, value, formatSpec.argWidth, formatSpec.numberWidth, formatSpec.leadingZero);
    }

    unsafe void IConverter<float>.Convert(ref char* dst, char* end, float value, FormatSpec formatSpec)
    {
        if (formatSpec.fractWidth == 0)
            formatSpec.fractWidth = 2;

        var intWidth = formatSpec.argWidth - formatSpec.fractWidth - 1;
        // Very crappy version for now
        bool neg = false;
        if (value < 0.0f)
        {
            neg = true;
            value = -value;
        }
        int v1 = Mathf.FloorToInt(value);
        float fractMult = (int)Mathf.Pow(10.0f, formatSpec.fractWidth);
        int v2 = Mathf.FloorToInt(value * fractMult) % (int)(fractMult);
        ConvertInt(ref dst, end, neg ? -v1 : v1, intWidth, formatSpec.numberWidth, formatSpec.leadingZero);
        if (dst < end)
            *dst++ = '.';
        ConvertInt(ref dst, end, v2, formatSpec.fractWidth, formatSpec.fractWidth, true);
    }

    unsafe void IConverter<string>.Convert(ref char* dst, char* end, string value, FormatSpec formatSpec)
    {
        int lpadding = 0, rpadding = 0;
        if (formatSpec.argWidth < 0)
            rpadding = -formatSpec.argWidth - value.Length;
        else
            lpadding = formatSpec.argWidth - value.Length;

        while (lpadding-- > 0 && dst < end)
            *dst++ = ' ';

        for (int i = 0, l = value.Length; i < l && dst < end; i++)
            *dst++ = value[i];

        while (rpadding-- > 0 && dst < end)
            *dst++ = ' ';
    }

    unsafe void IConverter<CharBufView>.Convert(ref char* dst, char* end, CharBufView value, FormatSpec formatSpec)
    {
        int lpadding = 0, rpadding = 0;
        if (formatSpec.argWidth < 0)
            rpadding = -formatSpec.argWidth - value.length;
        else
            lpadding = formatSpec.argWidth - value.length;

        while (lpadding-- > 0 && dst < end)
            *dst++ = ' ';

        for (int i = 0, l = value.length; i < l && dst < end; i++)
            *dst++ = value.buf[i+value.start];

        while (rpadding-- > 0 && dst < end)
            *dst++ = ' ';
    }

    unsafe void ConvertInt(ref char* dst, char* end, int value, int argWidth, int integerWidth, bool leadingZero)
    {
        // Dryrun to calculate size
        int numberWidth = 0;
        int signWidth = 0;
        int intpaddingWidth = 0;
        int argpaddingWidth = 0;

        bool neg = value < 0;
        if (neg)
        {
            value = -value;
            signWidth = 1;
        }

        int v = value;
        do
        {
            numberWidth++;
            v /= 10;
        }
        while (v != 0);

        if (numberWidth < integerWidth)
            intpaddingWidth = integerWidth - numberWidth;
        if (numberWidth + intpaddingWidth + signWidth < argWidth)
            argpaddingWidth = argWidth - numberWidth - intpaddingWidth - signWidth;

        dst += numberWidth + intpaddingWidth + signWidth + argpaddingWidth;

        if (dst > end)
            return;

        var d = dst;

        // Write out number
        do
        {
            *--d = (char)('0' + (value % 10));
            value /= 10;
        }
        while (value != 0);

        // Format width padding
        while (intpaddingWidth-- > 0)
            *--d = leadingZero ? '0' : ' ';

        // Sign if needed
        if (neg)
            *--d = '-';

        // Argument width padding
        while (argpaddingWidth-- > 0)
            *--d = ' ';
    }
}

public struct CharBufView
{
    public CharBufView(char[] buf, int start, int length)
    {
        this.buf = buf;
        this.start = start;
        this.length = length;
    }

    public CharBufView(char[] buf, int length)
    {
        this.buf = buf;
        this.length = length;
        this.start = 0;
    }

    public char[] buf;
    public int start;
    public int length;
}

/// <summary>
/// Garbage free string formatter
/// </summary>
public static class StringFormatter
{
    private class NoArg { };

    public static int Write(ref char[] dst, int destIdx, string format)
    {
        return Write<NoArg, NoArg, NoArg, NoArg, NoArg, NoArg>(ref dst, destIdx, format, null, null, null, null, null, null);
    }

    public static int Write<T0>(ref char[] dst, int destIdx, string format, T0 arg0)
    {
        return Write<T0, NoArg, NoArg, NoArg, NoArg, NoArg>(ref dst, destIdx, format, arg0, null, null, null, null, null);
    }

    public static int Write<T0, T1>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1)
    {
        return Write<T0, T1, NoArg, NoArg, NoArg, NoArg>(ref dst, destIdx, format, arg0, arg1, null, null, null, null);
    }

    public static int Write<T0, T1, T2>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        return Write<T0, T1, T2, NoArg, NoArg, NoArg>(ref dst, destIdx, format, arg0, arg1, arg2, null, null, null);
    }

    public static int Write<T0, T1, T2, T3>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        return Write<T0, T1, T2, T3, NoArg, NoArg>(ref dst, destIdx, format, arg0, arg1, arg2, arg3, null, null);
    }

    public static int Write<T0, T1, T2, T3, T4>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        return Write<T0, T1, T2, T3, T4, NoArg>(ref dst, destIdx, format, arg0, arg1, arg2, arg3, arg4, null);
    }

    public static int Write<T0,T1,T2,T3,T4,T5>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        int written = 0;
        unsafe
        {
            fixed (char* p = format, d = &dst[0])
            {
                var dest = d + destIdx;
                var end = d + dst.Length;
                var l = format.Length;
                var src = p;
                while (*src > 0 && dest < end)
                {
                    // Simplified parsing of {<argnum>[,<width>][:<format>]} where <format> is one of either 0000.00 or ####.## type formatters.
                    if (*src == '{' && *(src + 1) == '{')
                    {
                        *dest++ = *src++;
                        src++;
                    }
                    else if (*src == '}')
                    {
                        if (*(src + 1) == '}')
                        {
                            *dest++ = *src++;
                            src++;
                        }
                        else
                            throw new FormatException("You must escape curly braces");
                    }
                    else if (*src == '{')
                    {
                        src++;

                        // Default values of FormatSpec in case none are given in format string
                        FormatSpec s;
                        s.argWidth = 0;
                        s.numberWidth = 0;
                        s.fractWidth = 0;
                        s.leadingZero = false;

                        // Parse argument number
                        int argNum = 0;
                        argNum = ReadNum(ref src);

                        // Parse optional width
                        if (*src == ',')
                        {
                            src++;
                            s.argWidth = ReadNum(ref src);
                        }

                        // Parse optional format specifier 
                        if (*src == ':')
                        {
                            src++;
                            var ch = *src;
                            s.leadingZero = (ch == '0');
                            s.numberWidth = CountChar(ref src, ch);
                            if (*src == '.')
                            {
                                src++;
                                s.fractWidth = CountChar(ref src, ch);
                            }
                        }

                        // Skip to }
                        while (*src != '\0' && *src != '}')
                            src++;

                        if (*src == '\0')
                            throw new FormatException("Invalid format. Missing '}'?");
                        else
                            src++;

                        if (argNum == 0)
                        {
                            ((IConverter<T0>)Converter.instance).Convert(ref dest, end, arg0, s);
                        }

                        if (argNum == 1)
                        {
                            ((IConverter<T1>)Converter.instance).Convert(ref dest, end, arg1, s);
                        }

                        if (argNum == 2)
                        {
                            ((IConverter<T2>)Converter.instance).Convert(ref dest, end, arg2, s);
                        }

                        if (argNum == 3)
                        {
                            ((IConverter<T3>)Converter.instance).Convert(ref dest, end, arg3, s);
                        }

                        if (argNum == 4)
                        {
                            ((IConverter<T4>)Converter.instance).Convert(ref dest, end, arg4, s);
                        }

                        if (argNum == 5)
                        {
                            ((IConverter<T5>)Converter.instance).Convert(ref dest, end, arg5, s);
                        }
                    }
                    else
                    {
                        *dest++ = *src++;
                    }
                }
                written = (int)(dest - d + destIdx);
            }
        }

        return written;
    }

    static unsafe int ReadNum(ref char* p)
    {
        int res = 0;
        bool neg = false;
        if (*p == '-')
        {
            neg = true;
            p++;
        }
        while (*p >= '0' && *p <= '9')
        {
            res *= 10;
            res += (*p - '0');
            p++;
        }
        return neg ? -res : res;
    }

    static unsafe int CountChar(ref char* p, char ch)
    {
        int res = 0;
        while (*p == ch)
        {
            res++;
            p++;
        }
        return res;
    }
}