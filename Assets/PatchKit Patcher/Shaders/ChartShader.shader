Shader "PatchKit.Unity.Patcher/ChartShader"
{
    Properties
    {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1)
        _DefaultColor("_DefaultColor", Color) = (1.0, 1.0, 1.0, 1)
        _LineColor("_LineColor", Color) = (1.0, 1.0, 1.0, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _DefaultColor;
                float4 _LineColor;
                int _NumberOfSamples;
                float _StepHeight[1023];
                float _StepMax;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // draw line segment from A to B
            float segment(float2 P, float2 A, float2 B, float r)
            {
                float2 g = B - A;
                float2 h = P - A;
                float d = length(h - g * clamp(dot(g, h) / dot(g, g), 0.0, 1.0));
                return smoothstep(r, r * 0.5, d);
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float borderTopLine = 0.07;
                float borderFrontLine = 0.003;
                if (abs(_StepMax - i.uv.x) < borderFrontLine)
                {
                    return _LineColor;
                }

                if (i.uv.x > _StepMax)
                {
                    return _DefaultColor;
                }

                if (i.uv.y > _StepHeight[round(i.uv.x * _NumberOfSamples)])
                {
                    return _DefaultColor;
                }

                float4 fragColor = (i.uv.y) * _Color;
                fragColor = lerp(fragColor, _LineColor,
                                 segment(i.uv, float2(i.uv.x, _StepHeight[round(i.uv.x * _NumberOfSamples) - 1]),
                                         float2(i.uv.x, _StepHeight[round(i.uv.x * _NumberOfSamples)]), borderTopLine));
                fragColor = lerp(fragColor, _LineColor,
                                 segment(i.uv, float2(i.uv.x, _StepHeight[round(i.uv.x * _NumberOfSamples) + 1]),
                                         float2(i.uv.x, _StepHeight[round(i.uv.x * _NumberOfSamples)]), borderTopLine));
                return fragColor;
            }
            ENDCG
        }
    }
}