Shader "Leap Motion/AppModules/Voxel Binned Particles/Splat Display Example" {
    Properties{
        _Color     ("Color"       , Color) = (1,1,1,1)
        _MainTex   ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness"  , Range(0,1)) = 0.5
        _Metallic  ("Metallic"    , Range(0,1)) = 0.0
    }
        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
            #pragma target 5.0
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "./ParticleData.cginc"

            #ifdef SHADER_API_D3D11
                StructuredBuffer<SurfaceSplat> _SurfaceSplats;
            #endif

            struct appdata {
              uint instanceID : SV_InstanceID;

              float4 vertex : POSITION;
              float3 normal : NORMAL;
              float4 texcoord : TEXCOORD0;
              float4 texcoord1 : TEXCOORD1;
              float4 texcoord2 : TEXCOORD2;
              float4 color : COLOR;
            };

            struct Input {
                float2 uv_MainTex;
                float4 color : COLOR;
            };

            void setup() {
              #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
              SurfaceSplat splat = _SurfaceSplats[unity_InstanceID];

              float3 pos = splat.position;

              unity_ObjectToWorld._11_21_31_41 = float4(1, 0, 0, 0);
              unity_ObjectToWorld._12_22_32_42 = float4(0, 1, 0, 0);
              unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 1, 0);
              unity_ObjectToWorld._14_24_34_44 = float4(pos.xyz, 1);
              unity_WorldToObject = unity_ObjectToWorld;
              unity_WorldToObject._14_24_34 *= -1;
              unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
              #endif
            }

            void vert(inout appdata v, out Input o) {
              UNITY_INITIALIZE_OUTPUT(Input, o);

              #ifdef SHADER_API_D3D11
              SurfaceSplat p = _SurfaceSplats[v.instanceID];
              //float3 vel = p.position - p.prevPosition;
              v.vertex.xyz *= RADIUS * 0.6666;
              v.color = float4(p.color, 1);
              #else
              v.vertex = 0;
              v.color = 1;
              #endif
            }

            void surf(Input IN, inout SurfaceOutputStandard o) {
                o.Albedo = IN.color.rgb;
                o.Metallic = 0;
                o.Smoothness = 0;
                //o.Emission = IN.color.rgb;
            }
            ENDCG
        }
        FallBack "Diffuse"
}
