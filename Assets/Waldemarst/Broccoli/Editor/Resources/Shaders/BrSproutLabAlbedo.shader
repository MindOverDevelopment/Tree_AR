Shader "Hidden/Broccoli/SproutLabAlbedo"
{
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        
        _MinSproutTint ("Min Sprout Tint", Float) = 1
        _MaxSproutTint ("Max Sprout Tint", Float) = 1
        _TintColor ("Tint Color", Color) = (0.5,1,1,1)
        [Enum(Random,0,Hierarchy,1,Branch,2)] _SproutTintMode("Mode", Int) = 0
        _InvertSproutTintMode ("Sprout Tint Invert Mode", Float) = 0
        _SproutTintVariance ("Sprout Tint Variance", Float) = 0

        _MinSproutSat ("Min Sprout Saturation", Float) = 1
        _MaxSproutSat ("Max Sprout Saturation", Float) = 1
        [Enum(Random,0,Hierarchy,1,Branch,2)] _SproutSatMode("Mode", Int) = 0
        _InvertSproutSatMode ("Sprout Sat Invert Mode", Float) = 0
        _SproutSatVariance ("Sprout Sat Variance", Float) = 0
        _ApplyExtraSat ("Apply Saturation", Float) = 1

        _BranchShade ("Branch Shade", Float) = 1
        _BranchSat ("Branch Saturation", Float) = 1

        _IsLinearColorSpace ("Is Linear Color Space", Float) = 0
    }
    SubShader {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        LOD 100

        Cull Off
        Lighting Off
        //Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Name "Albedo"
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_fog
                
                #include "UnityCG.cginc"
                #include "BlendModes.hlsl"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed _Cutoff;

                float4 _TintColor;
                float _MinSproutTint;
                float _MaxSproutTint;
                uniform int _SproutTintMode;
                uniform int _InvertSproutTintMode;
                float _SproutTintVariance;
                float _RandSproutTint;
                float _PosSproutTint;
                float _SproutTint;

                float _BranchShade;
                float _BranchSat;

                float _MinSproutSat;
                float _MaxSproutSat;
                uniform int _SproutSatMode;
                uniform int _InvertSproutSatMode;
                float _SproutSatVariance;
                float _RandSproutSat;
                float _PosSproutSat;
                float _SproutSat;
                float _ApplyExtraSat;

                float _IsLinearColorSpace;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.color = v.color;
                    o.uv5 = v.uv5;
                    o.uv6 = v.uv6;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }
                
                fixed4 frag (v2f i) : Color
                {
                    fixed4 col = tex2D(_MainTex, i.texcoord);
                    
                    if (_IsLinearColorSpace) {
                        col.rgb = pow (col.rgb, 0.4545);
                        _TintColor.rgb = pow (_TintColor.rgb, 0.4545);
                    }
                    float4 texCol = col;

                    fixed4 vcol = i.color;
                    clip(col.a * vcol.a - _Cutoff);
                    //UNITY_APPLY_FOG(i.fogCoord, col);
                    // SHADE.
                    //col.rgb *= 1 - ((1 - i.color.r) / 2);

                    if (i.uv5.w == 1) { // Geometry is sprout, uv5.w == 1.
                        // TINT
                        _RandSproutTint = lerp (_MinSproutTint, _MaxSproutTint, i.color.g);
                        if (_SproutTintMode > 0) {
                            float sproutPos = (_SproutTintMode == 1?i.uv6.y:i.uv6.x);
                            if (_InvertSproutTintMode ==  0) _PosSproutTint = lerp (_MinSproutTint, _MaxSproutTint, sproutPos);
                            else _PosSproutTint = lerp (_MaxSproutTint, _MinSproutTint, sproutPos);
                            _SproutTint = lerp (_PosSproutTint, _RandSproutTint, _SproutTintVariance);
                        } else {
                            _SproutTint = _RandSproutTint;
                        }
                        float3 shiftedColor = BlendColor (col, lerp(col, _TintColor.rgb, _SproutTint));

                        // VIBRANCE PRESERVATION
                        half maxBase = max(col.r, max(col.g, col.b));
                        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
                        maxBase /= newMaxBase;
                        maxBase = maxBase * 0.5f + 0.5f;
                        shiftedColor.rgb *= maxBase;

                        // SATURATION
                        _RandSproutSat = lerp (_MinSproutSat, _MaxSproutSat, i.color.b);
                        if (_SproutSatMode > 0) {
                            float sproutPos = (_SproutSatMode == 1?i.uv6.y:i.uv6.x);
                            if (_InvertSproutSatMode ==  0) _PosSproutSat = lerp (_MinSproutSat, _MaxSproutSat, sproutPos);
                            else _PosSproutSat = lerp (_MaxSproutSat, _MinSproutSat, sproutPos);
                            _SproutSat = lerp (_PosSproutSat, _RandSproutSat, _SproutSatVariance);
                        } else {
                            _SproutSat = _RandSproutSat;
                        }
                        if (_ApplyExtraSat == 0) {
                            col.rgb = ContrastSaturationBrightness (shiftedColor, 1.0, _SproutSat, 1.0);
                        } else {
                            col.rgb = ContrastSaturationBrightness (shiftedColor, 1.1, _SproutSat, 1.0);
                        }
                    } else { // Geometry is branch, uv5.w == 0.
                        col.rgb = ContrastSaturationBrightness (col.rgb, 1, _BranchSat, 1);
                    }
                    
                    //col.rgb = BlendColor (texCol.rgb, col.rgb);

                    col.rgb = lerp (float3 (0,0,0), col.rgb, 1 - (1 - i.color.r) * 0.4);
                    
                    if (_IsLinearColorSpace)
                        col.rgb = pow (col.rgb, 2.2);

                    
                    return  col;
                }
                
            ENDHLSL
        }
    }
}