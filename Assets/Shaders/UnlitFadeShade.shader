Shader "Custom/UnlitFadeShade"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _ColorFondo("Color Fondo", Color) = (0,0,0,1)
        _TopY("Top Y", Float) = 1
        _BottomY("Bottom Y", Float) = -5
        _LightDir("Fake Light Dir (XYZ)", Vector) = (0.5, 1.0, 0.3, 0)
        _ShadeAmount("Shade Amount", Range(0, 1)) = 0.6
        _ShadeMin("Shade Min", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _ColorFondo;
                float  _TopY;
                float  _BottomY;
                float4 _LightDir;
                half   _ShadeAmount;
                half   _ShadeMin;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float ndl = saturate(dot(normalize(IN.normalWS), normalize(_LightDir.xyz)));
                float shade = lerp(_ShadeMin, 1.0, ndl);
                shade = lerp(1.0, shade, _ShadeAmount);

                half3 base = tex.rgb * _BaseColor.rgb * shade;

                float t = saturate((IN.positionWS.y - _BottomY) / (_TopY - _BottomY));
                half3 finalColor = lerp(_ColorFondo.rgb, base, t);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
