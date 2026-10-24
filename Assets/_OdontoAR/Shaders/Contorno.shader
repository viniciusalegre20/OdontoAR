// Contorno da estrutura selecionada (RF09): casca invertida, a malha inflada pela normal e só as faces
// de trás desenhadas, depois dos transparentes. A largura é em metros no mundo.
Shader "OdontoAR/Contorno"
{
    Properties
    {
        _BaseColor ("Cor", Color) = (0, 0.9, 1, 1)
        _Width ("Largura (m)", Float) = 0.0008
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+10" }

        Pass
        {
            Name "Contorno"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS + normalWS * _Width);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
