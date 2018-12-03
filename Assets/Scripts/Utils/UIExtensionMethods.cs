using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class UITextExtensionMethods
{
    static char[] buf = new char[1024];

    public static void Format<T0>(this UnityEngine.UI.Text me, string format, T0 arg0)
    {
        int l = StringFormatter.Write(ref buf, 0, format, arg0);
        me.Set(buf, l);
    }

    public static void Format<T0>(this TMPro.TextMeshProUGUI me, string format, T0 arg0)
    {
        int l = StringFormatter.Write(ref buf, 0, format, arg0);
        me.Set(buf, l);
    }

    public static void Format<T0, T1>(this UnityEngine.UI.Text me, string format, T0 arg0, T1 arg1)
    {
        int l = StringFormatter.Write(ref buf, 0, format, arg0, arg1);
        me.Set(buf, l);
    }

    public static void Format<T0, T1>(this TMPro.TextMeshProUGUI me, string format, T0 arg0, T1 arg1)
    {
        int l = StringFormatter.Write(ref buf, 0, format, arg0, arg1);
        me.Set(buf, l);
    }

    public static void Format<T0, T1, T2>(this UnityEngine.UI.Text me, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        int l = StringFormatter.Write(ref buf, 0, format, arg0, arg1, arg2);
        me.Set(buf, l);
    }

    public static void Format<T0, T1, T2>(this TMPro.TextMeshProUGUI me, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        int l = StringFormatter.Write(ref buf, 0, format, arg0, arg1, arg2);
        me.Set(buf, l);
    }

    public static void Set(this UnityEngine.UI.Text me, char[] text, int length)
    {
        var old = me.text;
        if (old.Length == length)
        {
            bool diff = false;
            for (var i = 0; i < length; i++)
            {
                if (text[i] != old[i])
                {
                    diff = true;
                    break;
                }
            }
            if (!diff)
                return;
        }
        me.text = new string(text, 0, length);
    }

    public static void Set(this TMPro.TextMeshProUGUI me, char[] text, int length)
    {
        var old = me.text;
        if (old.Length == length)
        {
            bool diff = false;
            for (var i = 0; i < length; i++)
            {
                if (text[i] != old[i])
                {
                    diff = true;
                    break;
                }
            }
            if (!diff)
                return;
        }
        me.text = new string(text, 0, length);
    }

    public static void SetRGB(this UnityEngine.UI.Graphic graphic, Color color)
    {
        var c = graphic.color;
        graphic.color = new Color(color.r, color.g, color.b, c.a);
    }
}
