namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [SkyUniqueID((int)SkyType.Gradient)]
    public class GradientSky : SkySettings
    {
        public ColorParameter top = new ColorParameter(Color.blue, true, false, true);
        public ColorParameter middle = new ColorParameter(new Color(0.3f, 0.7f, 1f), true, false, true);
        public ColorParameter bottom = new ColorParameter(Color.white, true, false, true);
        public FloatParameter gradientDiffusion = new FloatParameter(1);

        public override SkyRenderer CreateRenderer()
        {
            return new GradientSkyRenderer(this);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = 13;
                hash = hash * 23 + bottom.GetHashCode();
                hash = hash * 23 + top.GetHashCode();
                hash = hash * 23 + middle.GetHashCode();
                hash = hash * 23 + gradientDiffusion.GetHashCode();
            }

            return hash;
        }
    }
}
