// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Broccoli/SproutLabMix"
 {
     Properties
    {
        _MainTex ("Base (RGB), Alpha (A)", 2D) = "black" {}
        _BlendTex ("Base (RGB), Alpha (A)", 2D) = "black" {}
        _Color ("Tint Color", Color) = (0.6,0.5,0.5,0.5)
        _IsLinearColorSpace ("Is Linear Color Space", Float) = 0
        _BlendMode ("Blend Mode", Range(0, 29)) = 0
    }
 
    SubShader
    { 
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        LOD 100
     
        Cull Off
        Lighting Off
        //ZWrite Off
        //Fog { Mode Off }
        //Offset -1, -1
        //Blend SrcAlpha OneMinusSrcAlpha
        //Blend Off
 
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
             
            #include "UnityCG.cginc"
            #include "BlendModes.hlsl"
 
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
 
            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
 
            sampler2D _MainTex;
            sampler2D _BlendTex;
            float4 _Color;
            float _IsLinearColorSpace;
            float _BlendMode;
         
            v2f vert (appdata_t v)
            {
                //https://mouaif.wordpress.com/2009/01/05/photoshop-math-with-glsl-shaders/
                //https://github.com/cplotts/WPFSLBlendModeFx/blob/master/PhotoshopMathFP.hlsl
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }
             
            fixed4 frag (v2f i) : COLOR
            {
                float4 baseColor = tex2D(_MainTex, i.texcoord);
                float4 blendColor = tex2D(_BlendTex, i.texcoord);
                if (_IsLinearColorSpace) {
                    baseColor.rgb = pow (baseColor.rgb, 0.4545);
                    blendColor.rgb = pow (blendColor.rgb, 0.4545);
                }
                float4 hsbColor = baseColor;
                //hsbColor.rgb = BlendColor (baseColor.rgb, blendColor.rgb);
                hsbColor.rgb = BlendLinearLightf (baseColor.rgb, blendColor.rgb);

                switch (int(_BlendMode)) {
                    case 0: hsbColor.rgb = BlendNormal (baseColor.rgb, blendColor.rgb); break; // BlendNormal
                    case 1: hsbColor.rgb = BlendLighten (baseColor.rgb, blendColor.rgb); break; // BlendLighten 
                    case 2: hsbColor.rgb = BlendDarken (baseColor.rgb, blendColor.rgb); break; // BlendDarken
                    case 3: hsbColor.rgb = BlendMultiply (baseColor.rgb, blendColor.rgb); break; // BlendMultiply
                    case 4: hsbColor.rgb = BlendAverage (baseColor.rgb, blendColor.rgb); break; // BlendAverage
                    case 5: hsbColor.rgb = BlendAdd (baseColor.rgb, blendColor.rgb); break; // BlendAdd
                    case 6: hsbColor.rgb = BlendSubstract (baseColor.rgb, blendColor.rgb); break; // BlendSubstract
                    case 7: hsbColor.rgb = BlendDifference (baseColor.rgb, blendColor.rgb); break; // BlendDifference
                    case 8: hsbColor.rgb = BlendNegation (baseColor.rgb, blendColor.rgb); break; // BlendNegation
                    case 9: hsbColor.rgb = BlendExclusion (baseColor.rgb, blendColor.rgb); break; // BlendExclusion
                    case 10: hsbColor.rgb = BlendScreen (baseColor.rgb, blendColor.rgb); break; // BlendScreen
                    case 11: hsbColor.rgb = BlendOverlay (baseColor.rgb, blendColor.rgb); break; // BlendOverlay
                    case 12: hsbColor.rgb = BlendSoftLight (baseColor.rgb, blendColor.rgb); break; // BlendSoftLight
                    case 13: hsbColor.rgb = BlendHardLight (baseColor.rgb, blendColor.rgb); break; // BlendHardLight
                    case 14: hsbColor.rgb = BlendColorDodge (baseColor.rgb, blendColor.rgb); break; // BlendColorDodge
                    case 15: hsbColor.rgb = BlendColorBurn (baseColor.rgb, blendColor.rgb); break; // BlendColorBurn
                    case 16: hsbColor.rgb = BlendLinearDodge (baseColor.rgb, blendColor.rgb); break; // BlendLinearDodge
                    case 17: hsbColor.rgb = BlendLinearBurn (baseColor.rgb, blendColor.rgb); break; // BlendLinearBurn
                    case 18: hsbColor.rgb = BlendLinearLight (baseColor.rgb, blendColor.rgb); break; // BlendLinearLight
                    case 19: hsbColor.rgb = BlendVividLight (baseColor.rgb, blendColor.rgb); break; // BlendVividLight
                    case 20: hsbColor.rgb = BlendPinLight (baseColor.rgb, blendColor.rgb); break; // BlendPinLight
                    case 21: hsbColor.rgb = BlendHardMix (baseColor.rgb, blendColor.rgb); break; // BlendHardMix
                    case 22: hsbColor.rgb = BlendReflect (baseColor.rgb, blendColor.rgb); break; // BlendReflect
                    case 23: hsbColor.rgb = BlendGlow (baseColor.rgb, blendColor.rgb); break; // BlendGlow
                    case 24: hsbColor.rgb = BlendPhoenix (baseColor.rgb, blendColor.rgb); break; // BlendPhoenix
                    //case 25: hsbColor.rgb = BlendOpacity (baseColor.rgb, blendColor.rgb); break; // BlendOpacity
                    case 26: hsbColor.rgb = BlendHue (baseColor.rgb, blendColor.rgb); break; // BlendHue
                    case 27: hsbColor.rgb = BlendSaturation (baseColor.rgb, blendColor.rgb); break; // BlendSaturation
                    case 28: hsbColor.rgb = BlendColor (baseColor.rgb, blendColor.rgb); break; // BlendColor
                    case 31: hsbColor = BlendColorAlpha (baseColor, blendColor); break; // BlendColorAlpha
                    case 29: hsbColor.rgb = BlendLuminosity (baseColor.rgb, blendColor.rgb); break; // BlendLuminosity
                    case 30: hsbColor.rgb = BlendBlend (baseColor.rgb, blendColor.rgb); break; // BlendBlend
                }

                if (_IsLinearColorSpace)
                    hsbColor.rgb = pow (hsbColor.rgb, 2.2);
                return hsbColor;
            }
            ENDHLSL
        }
    }
  
     Fallback "Transparent/VertexLit"
 }
 
//BlendNormal       = 0
//BlendLighten      = 1
//BlendDarken       = 2
//BlendMultiply     = 3
//BlendAverage      = 4
//BlendAdd          = 5
//BlendSubstract    = 6
//BlendDifference   = 7
//BlendNegation     = 8
//BlendExclusion    = 9
//BlendScreen       = 10
//BlendOverlay      = 11
//BlendSoftLight    = 12
//BlendHardLight    = 13
//BlendColorDodge   = 14
//BlendColorBurn    = 15
//BlendLinearDodge  = 16
//BlendLinearBurn   = 17
//BlendLinearLight  = 18
//BlendVividLight   = 19
//BlendPinLight     = 20
//BlendHardMix      = 21
//BlendReflect      = 22
//BlendGlow         = 23
//BlendPhoenix      = 24
//BlendOpacity      = 25
//BlendHue          = 26
//BlendSaturation   = 27
//BlendColor        = 28
//BlendLuminosity   = 29
//BlendBlend        = 30