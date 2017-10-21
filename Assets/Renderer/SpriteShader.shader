Shader "NewChromantics/PopUnityBoy/SpriteShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		DisplaySpecificSprite("DisplaySpecificSprite", Range(-1,1023) ) = -1
		SpriteDisplayWidth("SpriteDisplayWidth", Range(1,11) ) = 11
		SpriteDisplayHeight("SpriteDisplayHeight", Range(1,12) ) = 12
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			int DisplaySpecificSprite;
			int SpriteDisplayWidth;
			int SpriteDisplayHeight;

	
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			#define COLOUR_INVALID_SPRITE	float4(1,0,1,1)

			fixed4 frag (v2f i) : SV_Target
			{
				int RenderWidth = SpriteDisplayWidth;
				int RenderHeight = SpriteDisplayHeight;
				int Renderx = RenderWidth * i.uv.x;
				int Rendery = RenderHeight * i.uv.y;
				int RenderIndex = Renderx + (RenderWidth * Rendery);

				if ( DisplaySpecificSprite != -1 )
				{
					RenderIndex = DisplaySpecificSprite;
					RenderWidth = 1;
					RenderHeight = 1;
				}

				if ( RenderIndex >= MAX_SPRITES )
					return COLOUR_INVALID_SPRITE;

				float Renderxf = RenderWidth * i.uv.x;
				float Renderyf = RenderHeight * i.uv.y;
				
				float Spriteu = fmod( Renderxf, 1 );
				float Spritev = 1 - fmod( Renderyf, 1 );
				float2 SpriteUv = float2(Spriteu,Spritev);

				int4 Sprite = GetSprite( RenderIndex );

				float4 Colour = GetSpriteColour( Sprite, SpriteUv );
			
				return Colour;
			}
			ENDCG
		}
	}
}
