Shader "NewChromantics/PopUnityBoy/PaletteShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			//#define PaletteTexture				_MainTex
			//#define PaletteTexture_TexelSize	_MainTex_TexelSize

			#include "UnityCG.cginc"
			#include "Gba.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};


			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
		

			fixed4 frag (v2f i) : SV_Target
			{
				int RenderWidth = 8;
				int RenderHeight = 8;
				int Renderx = RenderWidth * i.uv.x;
				int Rendery = RenderHeight * i.uv.y;
				int RenderIndex = Renderx + (RenderWidth * Rendery);

				float4 rgba = GetPalette15Colour( RenderIndex );
				return rgba;
			}
			ENDCG
		}
	}
}
