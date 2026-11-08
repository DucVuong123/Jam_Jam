Shader "JamJam/WoolBubble"
{
    Properties
    {
        _Tint("Tint",Color)=(0.75,0.9,1,0.06)
        _RimColor("Rim",Color)=(0.85,0.95,1,0.8)
        _RimPower("Rim Power",Range(0.5,8))=2.5
        _HighlightColor("Highlight",Color)=(1,1,1,0.9)
        _HighlightDirection("Highlight Direction",Vector)=(-0.45,0.55,0.7,0)
        _HighlightPower("Highlight Power",Range(4,256))=48
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Bubble"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _RimColor;
                half _RimPower;
                half4 _HighlightColor;
                float4 _HighlightDirection;
                half _HighlightPower;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 viewWS:TEXCOORD1;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.viewWS=GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 normalWS=normalize(input.normalWS);
                float3 viewWS=normalize(input.viewWS);
                half rim=pow(1.0h-saturate(dot(normalWS,viewWS)),_RimPower);
                float3 normalVS=TransformWorldToViewDir(normalWS,true);
                half spot=pow(saturate(dot(normalVS,normalize(_HighlightDirection.xyz))),_HighlightPower);
                half3 color=lerp(_Tint.rgb,_RimColor.rgb,rim);
                color=lerp(color,_HighlightColor.rgb,spot);
                half alpha=saturate(_Tint.a+rim*_RimColor.a+spot*_HighlightColor.a);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
