Shader "NewChromantics/PopUnityBoy/TileShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		VRamTexture("VRamTexture", 2D ) = "black" {}
		PaletteTexture("PaletteTexture", 2D ) = "black" {}
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


	
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			bool IsBackgroundLayerEnabled(int Layer)
			{
				//	dispcont bit 8+Layer
				return true;
			}
			bool IsObjectLayerEnabled()
			{
				return IsBackgroundLayerEnabled(4);
			}

			int GetBackgroundContextFromLayer(int Layer)
			{
			 	#define BG0CNT	8
				#define BG0CNT	10
				#define BG0CNT	12
				#define BG0CNT	14
				int Index = BG0CNT + Layer * 2;
				int BgContext = GetIoRam16( Index );
				BgContext = And3( BgContext );
				return BgContext;
			}

			void GetBackgroundLayerColour(int Layer,inout float4 Colour)
			{
				if ( !IsBackgroundLayerEnabled(Layer) )
					return;

				int BgContext = GetBackgroundContextFromLayer( Layer );				

				Colour.xyz = float3(0,0,1);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				//	clear colour
				float4 Colour = GetBgColour();

				GetBackgroundLayerColour( 0, Colour );

				return Colour;
			}
			ENDCG
		}
	}
}
