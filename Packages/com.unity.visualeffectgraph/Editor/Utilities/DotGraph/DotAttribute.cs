namespace UnityEditor.Dot
{
    public static class DotAttribute
    {
        public static readonly string Label         = "label";
        public static readonly string HeadLabel     = "headlabel";
        public static readonly string TailLabel     = "taillabel";
        public static readonly string Shape         = "shape";
        public static readonly string Color         = "color";
        public static readonly string Style         = "style";
    }

    public static class DotShape
    {
        public static readonly string None          = "plaintext";
        public static readonly string Box           = "box";
        public static readonly string Ellipse       = "ellipse";
        public static readonly string Square        = "square";
    }

    public static class DotColor
    {
        public static readonly string Black         = "black";
        public static readonly string White         = "white";
        public static readonly string Red           = "red";
        public static readonly string Green         = "green";
        public static readonly string Blue          = "blue";
        public static readonly string Cyan          = "cyan";
        public static readonly string Yellow        = "yellow";
        public static readonly string Orange        = "orange";
        public static readonly string SlateGray     = "lightslategray";
        public static readonly string Gray          = "gray";
        public static readonly string LightGray     = "lightgray";
        public static readonly string SteelBlue     = "steelblue";
    }

    public static class DotStyle
    {
        public static readonly string Filled        = "filled";
        public static readonly string Solid         = "solid";
        public static readonly string Dotted        = "dotted";
    }
}
