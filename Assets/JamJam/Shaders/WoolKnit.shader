Shader "JamJam/WoolKnit"
{
    Properties
    {
        _BaseMap("Knit",2D)="white" {}
        [Normal] _BumpMap("Knit Normal",2D)="bump" {}
        _BumpScale("Normal Scale",Float)=0.6
        _BaseColor("Tint",Color)=(1,1,1,1)
        _SideShade("Side Shade",Range(0,1))=0.7
        _BraidDensity("Braid Density",Float)=7
        _BraidDark("Braid Dark",Range(0,1))=0.62
        _Wrap("Light Wrap",Range(0,1))=0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half _BumpScale;
            half _SideShade;
            half _BraidDensity;
            half _BraidDark;
            half _Wrap;
        CBUFFER_END
        UNITY_INSTANCING_BUFFER_START(WoolKnitProps)
            UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
        UNITY_INSTANCING_BUFFER_END(WoolKnitProps)
        struct DepthAttributes
        {
            float4 positionOS:POSITION;
            float3 normalOS:NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct DepthVaryings
        {
            float4 positionCS:SV_POSITION;
            float3 normalWS:TEXCOORD0;
        };
        DepthVaryings DepthVert(DepthAttributes input)
        {
            DepthVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
            output.normalWS=TransformObjectToWorldNormal(input.normalOS);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                float4 tangentOS:TANGENT;
                float2 uv:TEXCOORD0;
                float2 edge:TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float2 uv:TEXCOORD0;
                float2 edge:TEXCOORD1;
                float3 positionWS:TEXCOORD2;
                float3 normalWS:TEXCOORD3;
                float4 tangentWS:TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                VertexPositionInputs position=GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal=GetVertexNormalInputs(input.normalOS,input.tangentOS);
                output.positionCS=position.positionCS;
                output.positionWS=position.positionWS;
                output.normalWS=normal.normalWS;
                output.tangentWS=float4(normal.tangentWS,input.tangentOS.w*GetOddNegativeScale());
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                output.edge=input.edge;
                return output;
            }
            half Braid(float2 edge)
            {
                float a=frac(edge.y*_BraidDensity+edge.x*0.5);
                float b=frac(edge.y*_BraidDensity-edge.x*0.5+0.5);
                float ridge=max(1.0-abs(a*2.0-1.0),1.0-abs(b*2.0-1.0));
                half braid=lerp(_BraidDark,1.0h,smoothstep(0.25,0.85,ridge));
                half groove=lerp(1.0h,_BraidDark,smoothstep(0.8,1.0,edge.x));
                return edge.x<0.999?braid*groove:1.0h;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 tint=UNITY_ACCESS_INSTANCED_PROP(WoolKnitProps,_BaseColor);
                half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*tint.rgb;
                float3 normalWS=normalize(input.normalWS);
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_BumpScale);
                float3 bitangent=input.tangentWS.w*cross(normalWS,input.tangentWS.xyz);
                float3 shadedNormal=normalize(TransformTangentToWorld(normalTS,half3x3(input.tangentWS.xyz,bitangent,normalWS)));
                albedo*=lerp(_SideShade,1.0h,saturate(normalWS.y));
                albedo*=Braid(input.edge);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half wrap=saturate((dot(shadedNormal,light.direction)+_Wrap)/(1.0+_Wrap));
                half3 lighting=light.color*wrap*light.shadowAttenuation+SampleSH(shadedNormal);
                return half4(albedo*lighting,1.0h);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float4 ShadowVert(DepthAttributes input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS=TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS=TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,_LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z=min(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z=max(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            half4 ShadowFrag():SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half4 DepthFrag(DepthVaryings input):SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            half4 NormalFrag(DepthVaryings input):SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS),0.0);
            }
            ENDHLSL
        }
    }
}
