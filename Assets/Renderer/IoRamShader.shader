Shader "NewChromantics/PopUnityBoy/IoRamShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		HighlightValue8("HighlightValue8", Range(-1,255) ) = 0
		HighlightValue16("HighlightValue16", Range(-1,65535) ) = 0
		ShowRamIndex("ShowRamIndex", Range(-1,1280) ) = -1
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

			int HighlightValue8;
			int HighlightValue16;
			int ShowRamIndex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

		
			fixed4 frag (v2f i) : SV_Target
			{
				int RenderWidth = IoRamTexture_TexelSize.z;
				int RenderHeight = IoRamTexture_TexelSize.w;	//	*4 per page
				int Renderx = RenderWidth * i.uv.x;
				int Rendery = RenderHeight * i.uv.y;
				int RenderIndex = Renderx + (RenderWidth * Rendery);

				if ( ShowRamIndex != -1 )
					RenderIndex = ShowRamIndex;

				int AlignedIndex = RenderIndex - (RenderIndex % 2);
				int Value16 = GetIoRam16( AlignedIndex );
				int Value8 = GetIoRam8( RenderIndex );

				if ( Value8 == HighlightValue8 )
					return float4( 0,1,0,1 );

				if ( Value16 != -1 && Value16 == HighlightValue16 )
					return float4( 0,1,1,1 );

				float Valuef = Value8 / 255.0f;
				return float4( Valuef, Valuef, Valuef, 1 );
			}
			ENDCG
		}
	}
}
