Shader "Custom/URP_Transparent_Emission" 
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0,1)) = 1.0
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,10)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _Color;
            float _Alpha;
            float4 _EmissionColor;
            float _EmissionStrength;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = tex2D(_BaseMap, i.uv) * _Color;
                c.a *= _Alpha;

                // HDR Emission output
                float3 emission = _EmissionColor.rgb * _EmissionStrength;

                // Add emission to final RGB
                c.rgb += emission;

                return c;
            }
            ENDHLSL
        }
    }
}
