Shader "NewChromantics/PopUnityBoy/TileShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		CharacterPage("CharacterPage", Range(0,4) ) = 0
		TileOffset("TileOffset", Range(0,1023) ) = 0
		TileDisplayWidth("TileDisplayWidth", Range(0,64) ) = 64
		TileDisplayHeight("TileDisplayHeight", Range(0,64) ) = 64

		PaletteRedStart("PaletteRedStart", Range(0,15) ) = 0
		PaletteGreenStart("PaletteGreenStart", Range(0,15) ) = 5
		PaletteBlueStart("PaletteBlueStart", Range(0,15) ) = 10
		PaletteEndianSwap("PaletteEndianSwap", Range(0,1) ) = 0

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

			int CharacterPage;
			int TileOffset;
			int TileDisplayWidth;
			int TileDisplayHeight;
	
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv = v.uv;
				return o;
			}

			bool IsBackgroundEnabled(int Layer)
			{
				//	dispcont bit 8+Layer
				return true;
			}
			bool IsObjectLayerEnabled()
			{
				return IsBackgroundEnabled(4);
			}

			#define BG0CNT	8
			#define BG1CNT	10
			#define BG2CNT	12
			#define BG2CNT	14
			#define BGXCNT(bg)	GetIoRam16( BG0CNT + (bg*2) )

			//	16 bit scroll pairs
			#define BG0HOFS	0x10
			#define BG0VOFS	0x12
			#define BGXHOFS(bg)	fmod( GetIoRam16( BG0HOFS + (bg*4) ), 511 )
			#define BGXVOFS(bg)	fmod( GetIoRam16( BG0VOFS + (bg*4) ), 511 )



			fixed4 frag (v2f i) : SV_Target
			{
				int RenderWidth = TileDisplayWidth;
				int RenderHeight = TileDisplayHeight;	//	*4 per page
				int Renderx = RenderWidth * i.uv.x;
				int Rendery = RenderHeight * i.uv.y;
				int RenderIndex = Renderx + (RenderWidth * Rendery);

				float Renderxf = RenderWidth * i.uv.x;
				float Renderyf = RenderHeight * i.uv.y;
				
				float Tileu = fmod( Renderxf, 1 );
				float Tilev = 1 - fmod( Renderyf, 1 );

				float4 Colour = GetTileColour( RenderIndex + TileOffset, float2(Tileu,Tilev), CharacterPage );
				return Colour;
			}
			ENDCG
		}
	}
}
