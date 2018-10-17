namespace UnityEngine.Experimental.Rendering
{
    public struct RectInt
    {
        public static readonly RectInt zero = new RectInt(0, 0, 0, 0);

        public int x;
        public int y;
        public int width;
        public int height;

        public RectInt(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
}
