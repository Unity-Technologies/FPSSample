Shader "Hidden/SRP_Core/TextureCombiner"
{
    Properties
    {
        // Chanels are : r=0, g=1, b=2, a=3, greyscale from rgb = 4
        // If the chanel value is negative, we invert the value

        [Linear][NoScaleOffset] _RSource ("R Source", 2D) = "white" {}
        _RChannel ("R Channel", float) = 0
        _RRemap ("R Remap", Vector) = (0, 1, 0, 0)

        [Linear][NoScaleOffset] _GSource ("G Source", 2D) = "white" {}
        _GChannel ("G Channel", float) = 1
        _GRemap ("G Remap", Vector) = (0, 1, 0, 0)

        [Linear][NoScaleOffset] _BSource ("B Source", 2D) = "white" {}
        _BChannel ("B Channel", float) = 2
        _BRemap ("B Remap", Vector) = (0, 1, 0, 0)

        [Linear][NoScaleOffset] _ASource ("A Source", 2D) = "white" {}
        _AChannel ("A Channel", float) = 3
        _ARemap ("A Remap", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _RSource, _GSource, _BSource, _ASource;
            float _RChannel, _GChannel, _BChannel, _AChannel;
            float4 _RRemap, _GRemap, _BRemap, _ARemap;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float PlotSourcetoChanel(float4 source, float param, float2 remap)
            {
                if (param < 0 )
                {
                    param = -param;
                    source = float4(1,1,1,1) - source;
                }

                float o;

                if (param >= 4)
                    o = source.r * 0.3 + source.g * 0.59 + source.b * 0.11; // Photoshop desaturation : G*.59+R*.3+B*.11
                else
                    o =  source[param];

                return o * ( remap.y - remap.x) + remap.x ;
            }

            float PlotSourcetoChanel(float4 source, float param)
            {
                return PlotSourcetoChanel(source, param, float2(0,1) );
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = float4(0,0,0,0);

                col.r = PlotSourcetoChanel( tex2D(_RSource, i.uv), _RChannel, _RRemap );
                col.g = PlotSourcetoChanel( tex2D(_GSource, i.uv), _GChannel, _GRemap );
                col.b = PlotSourcetoChanel( tex2D(_BSource, i.uv), _BChannel, _BRemap );
                col.a = PlotSourcetoChanel( tex2D(_ASource, i.uv), _AChannel, _ARemap );

                return col;
            }
            ENDCG
        }
    }
}
