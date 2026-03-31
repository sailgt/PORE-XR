Shader "Skybox/GridSkybox"
{
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (0,0,0,1)
        _GridColor ("Grid Color", Color) = (1,1,1,1)
        _GridScale ("Grid Scale", Float) = 20
        _LineWidth ("Line Width", Range(0.0001, 0.2)) = 0.02
        _LineSoftness ("Line Softness", Range(0.0, 0.2)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _BackgroundColor;
            float4 _GridColor;
            float _GridScale;
            float _LineWidth;
            float _LineSoftness;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);

                float3 dirVS = mul(UNITY_MATRIX_MV, float4(IN.positionOS, 0.0)).xyz;
                OUT.dirWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, dirVS));
                return OUT;
            }

            float2 DirToLatLongUV(float3 dir)
            {
                dir = normalize(dir);
                float u = atan2(dir.z, dir.x) * (1.0 / (2.0 * PI)) + 0.5;
                float v = asin(clamp(dir.y, -1.0, 1.0)) * (1.0 / PI) + 0.5;
                return float2(u, v);
            }

            float GridMask(float2 uv)
            {
                float2 p = frac(uv * _GridScale);
                float2 d2 = min(p, 1.0 - p);
                float d = min(d2.x, d2.y);
                float edge2 = _LineWidth + _LineSoftness;
                float t = smoothstep(_LineWidth, edge2, d);
                return 1.0 - t;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = DirToLatLongUV(IN.dirWS);
                float m = GridMask(uv);
                float3 col = lerp(_BackgroundColor.rgb, _GridColor.rgb, m);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}