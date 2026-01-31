Shader "Custom/FogSphere"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
        _FogStart ("Fog Start", Float) = 40
        _FogEnd ("Fog End", Float) = 70
        _NoiseScale ("Noise Scale", Float) = 0.05
        _NoiseStrength ("Noise Strength", Float) = 10
        _NoiseSpeed ("Noise Scroll Speed", Float) = 0.3
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+200"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "FogSphere"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float  _FogStart;
                float  _FogEnd;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _NoiseSpeed;
            CBUFFER_END

            // --- value noise ---
            float hash(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3D(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }

            float fbm(float3 p)
            {
                float v = 0.0;
                v += 0.500 * noise3D(p); p *= 2.01;
                v += 0.250 * noise3D(p); p *= 2.02;
                v += 0.125 * noise3D(p);
                return v / 0.875;
            }

            // --- vertex / fragment ---
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                OUT.worldPos   = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // scene depth
                float2 screenUV   = IN.screenPos.xy / IN.screenPos.w;
                float  rawDepth   = SampleSceneDepth(screenUV);
                float  linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // noise offset for organic boundary
                float3 noiseCoord = IN.worldPos * _NoiseScale + _Time.y * _NoiseSpeed;
                float  n = (fbm(noiseCoord) - 0.5) * _NoiseStrength;

                // depth-based fog
                float fogFactor = saturate((linearDepth - _FogStart + n) / (_FogEnd - _FogStart));

                // also fog where there is no geometry (sky / far plane)
                float sphereDist = distance(IN.worldPos, _WorldSpaceCameraPos);
                float skyFog = saturate((sphereDist - _FogStart + n) / (_FogEnd - _FogStart));
                fogFactor = max(fogFactor, skyFog);

                return half4(_FogColor.rgb, fogFactor);
            }
            ENDHLSL
        }
    }
}
