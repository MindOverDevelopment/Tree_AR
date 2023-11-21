Shader "Hidden/Broccoli/OutlineUnlit"
{
	Properties
	{
		_MainTex ("MainTex", 2D) = "white" {}
		_WireThickness ("Wire Thickness", RANGE(0, 800)) = 150
		_WireSmoothness ("Wire Smoothness", RANGE(0, 20)) = 3
		_WireColor ("Wire Color", Color) = (0.0, 1.0, 0.0, 1.0)
		_BaseColor ("Base Color", Color) = (0.0, 0.0, 0.0, 1.0)
		//_Width("Outline Width", Range(0,0.05)) = 0.003
		_Width("Outline Width", Range(0,0.05)) = 0.0015
		_TargetId("Outline Width", Float) = -1
		_OutlineTex ("Outline Texture", 2D) = "white" {}
		_OutlineColor("OutlineColor", Color) = (0.8,0.8,0.8,1)
		[Enum(Texture,0,ForwardGrad,1,SideGrad,2,CombinedGrad,3)] _Mode("Mode", Int) = 0 
	}

	SubShader
	{
		Tags {
			"IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            //"RenderType"="Opaque"
		}

		Pass
		{
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			// Wireframe shader based on the the following
			// http://developer.download.nvidia.com/SDK/10/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
			uniform float _WireThickness;
			uniform float _WireSmoothness;
			uniform float4 _WireColor; 
			uniform float4 _BaseColor;
			uniform int _Mode;

			fixed4 _OutlineColor;
			sampler2D _OutlineTex;
			float _Width;
			float _TargetId;
			float4 _baseColor = float4 (0,0,0,1);

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2g
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv4 : TEXCOORD4;
				float4 worldSpacePosition : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct g2f
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv4 : TEXCOORD4;
				float4 worldSpacePosition : TEXCOORD2;
				float4 dist : TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			v2g vert (appdata v)
			{
				v2g o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				o.uv0 = TRANSFORM_TEX(v.texcoord0, _MainTex);
				o.uv1 = v.texcoord1;
				o.uv4 = v.texcoord4;
				return o;
			}
			
			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
				float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
				float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				// To find the distance to the opposite edge, we take the
				// formula for finding the area of a triangle Area = Base/2 * Height, 
				// and solve for the Height = (Area * 2)/Base.
				// We can get the area of a triangle by taking its cross product
				// divided by 2.  However we can avoid dividing our area/base by 2
				// since our cross product will already be double our area.
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
				float wireThickness = 800 - _WireThickness;

				g2f o;
				
				o.uv0 = i[0].uv0;
				o.uv1 = i[0].uv1;
				o.uv4 = i[0].uv4;
				o.worldSpacePosition = i[0].worldSpacePosition;
				o.projectionSpaceVertex = i[0].projectionSpaceVertex;
				o.dist.xyz = float3( (area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[0], o);
				triangleStream.Append(o);

				o.uv0 = i[1].uv0;
				o.uv1 = i[1].uv1;
				o.uv4 = i[1].uv4;
				o.worldSpacePosition = i[1].worldSpacePosition;
				o.projectionSpaceVertex = i[1].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[1], o);
				triangleStream.Append(o);

				o.uv0 = i[2].uv0;
				o.uv1 = i[2].uv1;
				o.uv4 = i[2].uv4;
				o.worldSpacePosition = i[2].worldSpacePosition;
				o.projectionSpaceVertex = i[2].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[2], o);
				triangleStream.Append(o);
			}
			fixed4 frag (g2f i) : SV_Target
			{

				if (i.uv4.x != _TargetId) return _baseColor;

				float4 c = tex2D(_MainTex, i.uv0);

				float spriteLeft = tex2D(_MainTex, i.uv0 + float2(_Width, 0)).a;
                float spriteRight = tex2D(_MainTex, i.uv0 - float2(_Width,  0)).a;
				float spriteBottom = tex2D(_MainTex, i.uv0 + float2( 0 ,_Width)).a;
                float spriteTop = tex2D(_MainTex, i.uv0 - float2( 0 , _Width)).a;
				// then combine
				float result = (spriteRight + spriteLeft + spriteTop+ spriteBottom);
				// delete original alpha to only leave outline
				result *= (1-c.a);
				// add color and brightness
				float4 outlines = result * _OutlineColor;

				// only show outlines
				c = outlines;
				// show outlines +sprite
				//c.rgb = c.rgb + outlines;
 
				return  c ;
			}

			float power(float x, float y) {
				return exp(x * log(y));
			}
			ENDCG
		}
	}
}