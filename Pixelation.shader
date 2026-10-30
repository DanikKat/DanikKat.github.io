
Shader "Hidden/URP/Pixelation"
{
    Properties
    {
        _PixelSize ("Pixel Size", Float) = 6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Pixelation"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            // ВАЖНО: Core.hlsl должен идти первым!
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 resolution = _ScreenParams.xy;

                float size = max(1.0, floor(_PixelSize));

                float2 pixelCount = max(
                    floor(resolution / size),
                    float2(1.0, 1.0)
                );

                float2 uv =
                    (floor(input.texcoord * pixelCount) + 0.5)
                    / pixelCount;

                uv = saturate(uv);

                half4 color = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_PointClamp,
                    uv
                );

                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
