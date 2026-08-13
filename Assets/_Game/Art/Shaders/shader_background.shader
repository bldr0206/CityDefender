Shader "Custom/shader_background"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _Tint("Tint", Color) = (1, 1, 1, 1)
        _Scale("Texture Scale", Float) = 1
        _Movement("Movement (xy = direction/speed)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 screenUV : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Scale;
                float4 _Movement;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 0..1 screen-space position
                OUT.screenUV = OUT.positionHCS.xy / OUT.positionHCS.w * 0.5 + 0.5;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // aspect-correct so tiles stay square regardless of screen ratio
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = IN.screenUV * float2(aspect, 1.0) / max(_Scale, 1e-4);
                uv += _Movement.xy * _Time.y;

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _Tint;
                return color;
            }
            ENDHLSL
        }
    }
}
