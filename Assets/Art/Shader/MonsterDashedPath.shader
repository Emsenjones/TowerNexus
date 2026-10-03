Shader "TowerNexus/Monster Dashed Path"
{
    Properties
    {
        [MainTexture] _BaseMap("Dash Texture", 2D) = "white" {}
        _FlowSpeed("Flow Speed (Cycles Per Battle Second)", Float) = 0.5
        [HideInInspector] _FlowOffset("Flow Offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "DashedPathUnlit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DashedPathVertex
            #pragma fragment DashedPathFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                // Presenter reads speed and advances offset using scaled battle time.
                // Do not add Shader _Time animation: Draft pause must freeze the phase.
                float _FlowSpeed;
                float _FlowOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DashedPathVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Material authoring applies tiling/offset exactly once.
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.color = input.color;
                return output;
            }

            half4 DashedPathFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;
                // Increasing instance phase moves the pattern toward increasing route U:
                // Spawn (first LineRenderer node) -> Target (last node).
                uv.x -= _FlowOffset;
                half4 pattern = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                // Texture Alpha supplies the gaps; LineRenderer RGBA supplies state tint/opacity.
                return pattern * input.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
