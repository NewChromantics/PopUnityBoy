Shader "NewChromantics/PopUnityBoy/BackgroundShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		//VRamTexture("VRamTexture", 2D ) = "black" {}
		//IoRamTexture("IoRamTexture", 2D ) = "black" {}
		//PaletteTexture("PaletteTexture", 2D ) = "black" {}
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
			#define BGXCNT(bg)	GetIoRam16( BG0CNT + ((bg)*2) )

			//	16 bit scroll pairs
			#define BG0HOFS	0x10
			#define BG0VOFS	0x12
			#define BGXHOFS(bg)	fmod( GetIoRam16( BG0HOFS + (bg*4) ), 511 )
			#define BGXVOFS(bg)	fmod( GetIoRam16( BG0VOFS + (bg*4) ), 511 )

			int GetBackgroundPriority(int Background)
			{
				int BgContext = BGXCNT( Background );
				int Priority = BgContext & 0x3;
				return Priority;
			}

			void GetBackgroundColour(int Background,int2 xy,inout float4 Colour)
			{
			#define RETURN_ERROR	{	Colour.xyz = float3(1,0,0);		return;	}
				if ( !IsBackgroundEnabled(Background) )
					return;

				//	z order, match with sprites!
				int BgContext = BGXCNT( Background );
				int ResolutionMode = (BgContext >> 14) & 3;
				if ( Background == 0 && ResolutionMode != 0 )
					RETURN_ERROR;
				int2 Resolutions[4];
				Resolutions[0] = int2( 256, 256 );
				Resolutions[1] = int2( 512, 256 );
				Resolutions[2] = int2( 256, 512 );
				Resolutions[3] = int2( 512, 512 );
				int2 Resolution = Resolutions[ResolutionMode];
				int width = Resolution.x;
				int height = Resolution.y;

				int screenBase = And31( RightShift(BgContext,8) )  * 0x800;
				int charBase = And3( RightShift(BgContext,2) ) * 0x4000;
				int hofs = BGXHOFS(Background);
				int vofs = BGXVOFS(Background);
				//hofs = 0;
				//vofs = 0;

				bool TileColourCount256 = (BgContext & (1<<7)) != 0;
				if ( !TileColourCount256 )
					RETURN_ERROR;
			
                int bgy = ((xy.y + vofs) & (height - 1)) / 8;
				int tileIdx = screenBase + (((bgy & 31) * 32) * 2);
				int TileSizeMode = (BgContext >> 14) & 0x3;
				switch (TileSizeMode)
                {
                    case 2: 
                    if (bgy >= 32) 
                   		tileIdx += 32 * 32 * 2; 
                    break;

                    case 3: 
                    if (bgy >= 32) 
                    	tileIdx += 32 * 32 * 4; 
                    break;
                }

               	//	gr: get rid of all these magic numbers!
                int tileY = ((xy.y + vofs) & 0x7) * 8;

                int i = xy.x;
                int bgx = ((i + hofs) & (width - 1)) / 8;
                int tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                if (bgx >= 32) 
                	tmpTileIdx += 32 * 32 * 2;

                int tileChar = GetVRam16( tmpTileIdx );
                int x = (i + hofs) & 7;
                int y = tileY;
                if ((tileChar & (1 << 10)) != 0) 
                	x = 7 - x;
                if ((tileChar & (1 << 11)) != 0) 
               		y = 56 - y;

                int TileVRamIndex = charBase + ((tileChar & 0x3FF) * (8*8)) + y + x;
                int PaletteIndex = GetVRam8( TileVRamIndex );
                if ( PaletteIndex != 0 )
                {
                	//	todo: get blend mode

                	Colour.xyz = GetPalette15Colour( PaletteIndex );
                }
			}

			float Range(float Value,float Min,float Max)
			{
				return (Value-Min) / (Max-Min);
			}

			float2 Range2(float2 Value,float2 Min,float2 Max)
			{
				float x = Range( Value.x, Min.x, Max.x );
				float y = Range( Value.y, Min.y, Max.y );
				return float2( x, y );
			}

			float2 GetRectUv(float2 Position,float2 RectPos,float2 RectSize)
			{
				return Range2( Position, RectPos, RectPos + RectSize );
			}

			bool IsInside01(float Value)
			{
				return (Value>=0) && (Value<1);
			}

			//	gr: a pre-sort step for sprites would be good for optimisation
			void GetSpriteLayerColour(int PriorityFilter,int2 xy,inout float4 Colour)
			{
				int DisplayContext = 0;
				DisplayContext |= 1 << 12;

				//	OBJ disabled
				bool ObjEnabled = ( DisplayContext & (1 << 12) ) != 0;
				if ( !ObjEnabled )
					return;

				//	sprites render smallest index on top
				//for ( int s=MAX_SPRITES-1;	s>=0;	s-- )
				for ( int s=20-1;	s>=0;	s-- )
				{
					bool Valid = true;
					int4 Sprite = GetSprite(s);
					int SpritePriority = GetSpritePriority(Sprite);
					Valid = Valid && ( SpritePriority == PriorityFilter );

					int2 SpritePos = GetSpritePos( Sprite );
					int2 SpriteSize = GetSpriteSize( Sprite );
					float2 SpriteUv = GetRectUv( xy, SpritePos, SpriteSize );
					Valid = Valid && IsInside01( SpriteUv.x ) && IsInside01( SpriteUv.y );

					float4 SpriteColour = GetSpriteColour( Sprite, SpriteUv );

					//	blend sprite colour with alpha
					float Alpha = SpriteColour.w;
					//SpriteColour.xyz = (Colour.xyz*(1-Alpha)) + (SpriteColour.xyz*(Alpha));
					Valid = Valid && (Alpha > 0);

					Colour.xyz = lerp( Colour, SpriteColour, Valid );

					/*
					if ((this.dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

		                // Y clipping
		                if (y > ((y + rheight) & 0xff))
		                {
		                    if (this.curLine >= ((y + rheight) & 0xff) && !(y < this.curLine)) continue;
		                }
		                else
		                {
		                    if (this.curLine < y || this.curLine >= ((y + rheight) & 0xff)) continue;
		                }
					*/
				}
			}


			fixed4 frag (v2f i) : SV_Target
			{
				//	clear colour
				float4 Colour = GetBgColour();
				float2 ScreenSize = float2( 240, 160 );
				float2 uv = float2( i.uv.x, 1-i.uv.y );
				int2 xy = uv * ScreenSize;

				//	draw backgrounds in their priority order
				int BackgroundOrder[4];
				BackgroundOrder[ GetBackgroundPriority(0) ] = 0;
				BackgroundOrder[ GetBackgroundPriority(1) ] = 1;
				BackgroundOrder[ GetBackgroundPriority(2) ] = 2;
				BackgroundOrder[ GetBackgroundPriority(3) ] = 3;

				GetBackgroundColour( BackgroundOrder[3], xy, Colour );
				GetSpriteLayerColour( 3, xy, Colour );

				GetBackgroundColour( BackgroundOrder[2], xy, Colour );
				GetSpriteLayerColour( 2, xy, Colour );

				GetBackgroundColour( BackgroundOrder[1], xy, Colour );
				GetSpriteLayerColour( 1, xy, Colour );

				GetBackgroundColour( BackgroundOrder[0], xy, Colour );
				GetSpriteLayerColour( 0, xy, Colour );

				return Colour;
			}
			ENDCG
		}
	}
}
