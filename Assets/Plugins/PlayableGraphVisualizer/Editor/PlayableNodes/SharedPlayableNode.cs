using System;

namespace GraphVisualizer
{
    public class SharedPlayableNode : Node
    {
        public SharedPlayableNode(object content, float weight = 1.0f, bool active = false)
            : base(content, weight, active)
        {
        }

        protected static string InfoString(string key, double value)
        {
            if (Math.Abs(value) < 100000.0)
                return string.Format("<b>{0}:</b> {1:#.###}", key, value);
            if (value == double.MaxValue)
                return string.Format("<b>{0}:</b> +Inf", key);
            if (value == double.MinValue)
                return string.Format("<b>{0}:</b> -Inf", key);
            return string.Format("<b>{0}:</b> {1:E4}", key, value);
        }

        protected static string InfoString(string key, int value)
        {
            return string.Format("<b>{0}:</b> {1:D}", key, value);
        }

        protected static string InfoString(string key, object value)
        {
            return "<b>" + key + ":</b> " + (value ?? "(none)");
        }

        protected static string RemoveFromEnd(string str, string suffix)
        {
            if (str.EndsWith(suffix))
            {
                return str.Substring(0, str.Length - suffix.Length);
            }

            return str;
        }
    }
}