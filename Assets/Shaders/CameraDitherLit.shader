Shader "Moon/CameraDitherLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map",2D)="white"{}
        [MainColor] _BaseColor("Base Color",Color)=(1,1,1,1)
        [Normal] _BumpMap("Normal",2D)="bump"{}
        _BumpScale("Normal Strength",Float)=1
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Smoothness",Range(0,1))=.2
        [HDR] _EmissionColor("Emission",Color)=(0,0,0,0)
        _CameraFade("Camera Visibility",Range(0,1))=1
        _FadeCenter("Fade Screen Center",Vector)=(.5,.5,0,0)
        _FadeRadius("Fade Screen Radius",Vector)=(.18,.25,0,0)
        _Cull("Cull",Float)=2
        _Cutoff("Alpha Cutoff",Range(0,1))=.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half4 _EmissionColor;
        half _BumpScale, _Metallic, _Smoothness, _CameraFade;
        float4 _FadeCenter, _FadeRadius;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; half fog:TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
        Varyings Vert(Attributes v)
        {
            Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
            VertexNormalInputs n=GetVertexNormalInputs(v.normalOS,v.tangentOS);
            o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=n.normalWS;
            o.tangentWS=half4(n.tangentWS,v.tangentOS.w*GetOddNegativeScale());
            o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
        }
        void Dither(float4 positionCS)
        {
            float2 uv=positionCS.xy/_ScaledScreenParams.xy;
            float radius=length((uv-_FadeCenter.xy)/max(_FadeRadius.xy,float2(.01,.01)));
            float mask=1-smoothstep(.7,1,radius);
            float visibility=lerp(1,_CameraFade,mask);
            float noise=frac(52.9829189*frac(dot(floor(positionCS.xy),float2(.06711056,.00583715))));
            clip(visibility-noise-.00001);
        }
        half3 Normal(Varyings i)
        {
            half3 n=normalize(i.normalWS),t=normalize(i.tangentWS.xyz);
            half3 b=cross(n,t)*i.tangentWS.w;
            half3 ts=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
            return NormalizeNormalPerPixel(TransformTangentToWorld(ts,half3x3(t,b,n)));
        }
        half4 Frag(Varyings i):SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);Dither(i.positionCS);
            InputData data=(InputData)0;data.positionWS=i.positionWS;data.normalWS=Normal(i);
            data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
            data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.fogCoord=i.fog;
            data.vertexLighting=VertexLighting(i.positionWS,data.normalWS);data.bakedGI=SampleSH(data.normalWS);
            data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
            SurfaceData surface=(SurfaceData)0;surface.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
            surface.alpha=1;surface.metallic=_Metallic;surface.smoothness=_Smoothness;
            surface.specular=half3(.04,.04,.04);surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.emission=_EmissionColor.rgb;
            half4 color=UniversalFragmentPBR(data,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
        }
        half4 Depth(Varyings i):SV_Target {Dither(i.positionCS);return 0;}
        half4 Normals(Varyings i):SV_Target
        {
            Dither(i.positionCS);half3 n=Normal(i);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
            #else
                return half4(n,0);
            #endif
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForwardOnly"}
            ZWrite On
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Depth
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormalsOnly"}
            ZWrite On
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Normals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
        // Occlusion changes visibility, not physical shelter or its shadow.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
