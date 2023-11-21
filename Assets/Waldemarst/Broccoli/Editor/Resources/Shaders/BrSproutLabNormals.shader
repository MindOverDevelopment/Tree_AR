// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/Broccoli/SproutLabNormals"
{
    // https://blog.selfshadow.com/publications/blending-in-detail/
    Properties {
        // three textures we'll use in the material
        _MainTex("Base texture", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Cutoff  ("Cutoff", Float) = 0.5
        _IsLinearColorSpace ("Is Linear Color Space", Float) = 1
        _IsGammaDisplay ("Is Gamma Display", Float) = 1
    }
    SubShader
    {
        Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}
        LOD 200

        Cull Off
        Lighting Off
        ZWrite On
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            // exactly the same as in previous shader
            struct v2f {
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBitangent : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 color : COLOR;
                float4 pos : SV_POSITION;
            };

            // textures from shader properties
            sampler2D _MainTex;
            sampler2D _BumpMap;
            float _Cutoff;
            float _IsLinearColorSpace;
            float _IsGammaDisplay;

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL, float4 tangent : TANGENT, float2 uv : TEXCOORD0, float4 color : COLOR)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                float3 wNormal = UnityObjectToWorldNormal(normal);
                wNormal = RotateAroundYInDegrees (wNormal, 90.0);
                float3 wTangent = UnityObjectToWorldDir(tangent.xyz);
                float tangentSign = tangent.w * unity_WorldTransformParams.w;
                float3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                o.uv = uv;
                o.color = color;

                float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
                o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                o.worldNormal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;
                
                o.worldTangent = mul(unity_ObjectToWorld, float4(tangent.xyz, 0)).xyz;
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) * tangent.w;

                return o;
            }
            
            float4 frag (v2f i, fixed facing : VFACE) : COLOR
            {
                float3 worldNormal = UnpackNormal(tex2D(_BumpMap, i.uv));
                //worldNormal = pow(worldNormal, 2.2);
                
                float3 baseNormal = float3(
                    dot(UNITY_MATRIX_V[0].xyz, i.worldNormal),
                    dot(UNITY_MATRIX_IT_MV[1].xyz, i.worldNormal), 
                    dot(UNITY_MATRIX_IT_MV[2].xyz, i.worldNormal));
                baseNormal *= facing;

                //float3 mixNormal = normalize(float3(baseNormal.xy + worldNormal.xy, baseNormal.z));
                float3 mixNormal = normalize(float3(baseNormal.xy + worldNormal.xy, baseNormal.z));

                float4 color = float4(mixNormal, 1);
                color = color * 0.5 + 0.5;

                float4 _talbedo = tex2D(_MainTex, i.uv);
                clip(color.a * _talbedo.a - _Cutoff);
                
                if (_IsLinearColorSpace == 0) {
                    color.rgb = pow(color.rgb, 0.45);
                }
                if (_IsGammaDisplay == 1) {
                    color.rgb = pow(color.rgb, 2.2);
                }

                return color;
            }
            ENDHLSL
        }
    }
}