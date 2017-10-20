#if !defined(VRamTexture)
sampler2D VRamTexture;
float4 VRamTexture_TexelSize;
#endif

#if !defined(PaletteTexture)
sampler2D PaletteTexture;
float4 PaletteTexture_TexelSize;
#endif

#if !defined(IoRamTexture)
sampler2D IoRamTexture;
float4 IoRamTexture_TexelSize;
#endif

#if !defined(OamRamTexture)
sampler2D OamRamTexture;
float4 OamRamTexture_TexelSize;
#endif


/*	gr: if these are not in the parent shader then all zero..
int PaletteRedStart = 0;
int PaletteGreenStart = 5;
int PaletteBlueStart = 10;
int PaletteEndianSwap = 0;
*/
#define PaletteRedStart			0
#define PaletteGreenStart		5
#define PaletteBlueStart		10
#define PaletteEndianSwap		0


#define MAX_SPRITES			128


int RightShift(int Value,int ShiftAmount)	
{
	return Value >> ShiftAmount;
	//return floor(f / pow(2,ShiftAmount) );	
}

int LeftShift(int Value,int ShiftAmount)	
{
	return Value << ShiftAmount;
	//return f * pow(2,ShiftAmount);	
}

int And3(int Value)
{
	return (Value & 3);
	//Value = fmod( Value, 3 );
	//return Value;
}

int And1(int Value)
{
	return (Value & 1);
	//Value = fmod( Value, 1 );
	//return Value;
}


int And31(int Value)
{
	return (Value & 31);
	//Value = fmod( Value, 3 );
	//return Value;
}


//	adapted from https://stackoverflow.com/a/27551433
float4 UnpackColor15(int f) 
{
/*
	float a = RightShift( f, 15 );
	float r = RightShift( f - LeftShift(a,15), 10 );
	float g = RightShift( f - LeftShift(r,10), 5 );
	float b = floor( f - LeftShift(r,10) - LeftShift(g,5) );
*/
	int Colour16 = f;
	/*
	int a = (Colour16 >> 15) & 1;
	int r = (Colour16 >> 10) & 31;
	int g = (Colour16 >> 5) & 31;
	int b = (Colour16 >> 0) & 31;
	*/
	int a = 1;
	int r = (Colour16 >> PaletteRedStart) & 31;
	int g = (Colour16 >> PaletteGreenStart) & 31;
	int b = (Colour16 >> PaletteBlueStart) & 31;

	//	normalize
	float Max5 = 31;
	float4 rgba = float4( r,g,b,a ) / float4( Max5, Max5, Max5, 1 );
	rgba = clamp( 0, 1, rgba );

	//	gr: alpha is always 0?
	//a = 1;


	return rgba;
}

float4 GetPalette15Colour(int Index)
{
	int SourceWidth = PaletteTexture_TexelSize.z;
	int SourceHeight = PaletteTexture_TexelSize.w;
	int Samplex = Index % SourceWidth;
	int Sampley = Index / SourceWidth;
	float2 Sampleuv = float2(Samplex,Sampley) / float2(SourceWidth,SourceHeight);

	//	scale float data to 16 bit
	//	gr: this doesnt work - maybe too big for floats? colours seem to bleed
	/*
	int2 Scalar = ( PaletteEndianSwap ) ? int2( 256, 256*256 ) : int2( 256*256, 256 );
	int2 PaletteColour8_8 = tex2D(PaletteTexture, Sampleuv).xy * Scalar;
	int PaletteColour16 = PaletteColour8_8.x + PaletteColour8_8.y;
	*/
	//	gr: works!
	int2 PaletteColour8_8 = tex2D(PaletteTexture, Sampleuv).xy * 256;
	int2 Scalar = ( PaletteEndianSwap ) ? int2( 256, 1 ) : int2( 1, 256 );
	int PaletteColour16 = (PaletteColour8_8.x*Scalar.x) + (PaletteColour8_8.y*Scalar.y);

	float4 rgba = UnpackColor15( PaletteColour16 );
	return rgba;
}

int GetVRam8(int Index)
{
	float w = VRamTexture_TexelSize.z;
	float h = VRamTexture_TexelSize.w;
	float u = (Index % w) / w;
	float v = (Index / w) / h;

	int Value = tex2D( VRamTexture, float2(u,v) ) * 256;

	return Value;
}

int GetVRam16(int Index)
{
	int x = GetVRam8( Index ) ;
	x += GetVRam8( Index+1 ) * 256;
	return x;
}


int GetIoRam8(int Index)
{
	float w = IoRamTexture_TexelSize.z;
	float h = IoRamTexture_TexelSize.w;
	float u = (Index % w) / w;
	float v = (Index / w) / h;

	int Value = tex2D( IoRamTexture, float2(u,v) ).x * 256.0f;

	return Value;
}

int GetIoRam16(int Index)
{
	int a = GetIoRam8( Index+0 );
	int b = GetIoRam8( Index+1 ) * 256;
	return a+b;
}


int GetOamRam8(int Index)
{
	float w = OamRamTexture_TexelSize.z;
	float h = OamRamTexture_TexelSize.w;
	float u = (Index % w) / w;
	float v = (Index / w) / h;

	int Value = tex2D( OamRamTexture, float2(u,v) ).x * 256.0f;

	return Value;
}

int GetOamRam16(int Index)
{
	int a = GetOamRam8( Index+0 );
	int b = GetOamRam8( Index+1 ) * 256;
	return a+b;
}


//	mode 1 stuff
float4 GetBgColour()
{
	return GetPalette15Colour( 0 );
}


float4 GetTileColour(int TileIndex,float2 Tileuv,int CharacterSet)
{
	int Tilex = 8.0f * Tileuv.x;
	int Tiley = 8.0f * Tileuv.y;

	//return float4( TileIndex/2048.0f,0,0, 1);

	//	this many bytes in vram 131072
	//	2048 possible tiles
	//	int CharacterSet = (BgContext >> 2) & 3;
	int CharAddressBase = CharacterSet * 16384;//0x4000;
	//0x10000=65536
	int VramIndex = CharAddressBase + TileIndex * (8*8);
	int x = Tilex;
	int y = Tiley;
	VramIndex += x;
	VramIndex += y*8;
	int PaletteIndex = GetVRam8( VramIndex );
	//return float4( PaletteIndex / 256.0f, 0, 0, 1);
	//int pal = tex2D( VRamTexture, i.uv ).r * 256;
	float4 rgba = GetPalette15Colour( PaletteIndex );
	return float4( rgba.xyz,1 );
}


int4 GetSprite(int SpriteIndex)
{
	//SpriteIndex *= 32;
	SpriteIndex *= (16 * 4)/8;
	int4 SpriteAttribs;
	SpriteAttribs[3] = GetOamRam16( SpriteIndex + 6 );
	SpriteAttribs[2] = GetOamRam16( SpriteIndex + 4 );
	SpriteAttribs[1] = GetOamRam16( SpriteIndex + 2 );
	SpriteAttribs[0] = GetOamRam16( SpriteIndex + 0 );
	return SpriteAttribs;
}

int GetSpritePriority(int4 Sprite)
{
	int Priority = (Sprite[2] >> 10) & 3;
	return Priority;

}

int2 GetSpritePos(int4 Sprite)
{
	int x = Sprite[1] & 0x1FF;
	int y = Sprite[0] & 0xFF;
	return int2( x,y );
}

float GetSpriteAlpha(int4 Sprite)
{
	int BlendMode = (Sprite[0] >> 10) & 3;
	float BlendAlphas[3];
	BlendAlphas[0] = 1;
	BlendAlphas[1] = 0.5f;
	BlendAlphas[2] = 1;	//	Obj window
	BlendAlphas[3] = 1;
	return BlendAlphas[BlendMode];
}
	
int2 GetSpriteSize(int4 Sprite)
{
	int SizeMode = (Sprite[0] >> 14) & 3;
	int Scale = (Sprite[1] >> 14) & 3;
	#define SIZE_MODE_SQUARE	0
	#define SIZE_MODE_RECTHORZ	1
	#define SIZE_MODE_RECTVERT	2
	#define SIZE_MODE_UNKNOWN	3

	int2 Size = int2(8,8);

	if ( SizeMode == SIZE_MODE_SQUARE )
	{
		int2 Scales[4];
		Scales[0] = int2( 8,8 );
		Scales[1] = int2( 16,16 );
		Scales[2] = int2( 32,32 );
		Scales[3] = int2( 64,64 );
		Size = Scales[Scale];
	}
	else if ( SizeMode == SIZE_MODE_RECTHORZ )
	{
		int2 Scales[4];
		Scales[0] = int2( 16,8 );
		Scales[1] = int2( 32,16 );
		Scales[2] = int2( 32,16 );
		Scales[3] = int2( 64,32 );
		Size = Scales[Scale];
	}
	else if ( SizeMode == SIZE_MODE_RECTVERT )
	{
		int2 Scales[4];
		Scales[0] = int2( 8,16 );
		Scales[1] = int2( 8,32 );
		Scales[2] = int2( 16,32 );
		Scales[3] = int2( 32,64 );
		Size = Scales[Scale];
	}

	bool DoubleSize = (Sprite[0] & (1<<8)) != 0;
	bool RotateScaleEnabled = (Sprite[0] & (1<<9)) != 0;
	if ( DoubleSize && RotateScaleEnabled )
	{
		Size *= 2;
	}
	else if ( RotateScaleEnabled )
	{
		//	invalid sprite.... on purpose?
	}

	//	gr: another scale?
	/*
	int scale = 1;
	if ((attr0 & (1 << 13)) != 0) 
		scale = 2;
	*/

	return Size;
}

int GetSpriteTileIndex(int4 Sprite)
{
	int TileIndex = Sprite[2] & 0x3FF;
	return TileIndex;
}




float4 GetSpriteTileColour(int TileIndex,float2 Tileuv,int CharacterSet)
{
	int Tilex = 8.0f * Tileuv.x;
	int Tiley = 8.0f * Tileuv.y;

	//return float4( TileIndex/2048.0f,0,0, 1);

	//	this many bytes in vram 131072
	//	2048 possible tiles
	//	int CharacterSet = (BgContext >> 2) & 3;
	int CharAddressBase = (1+CharacterSet) * 0x10000;
	//0x10000=65536
	int VramIndex = CharAddressBase + (TileIndex * 32);
	int x = Tilex;
	int y = Tiley;
	VramIndex += x;
	VramIndex += y*8;
	int PaletteIndex = GetVRam8( VramIndex );
	//return float4( PaletteIndex / 256.0f, 0, 0, 1);
	//int pal = tex2D( VRamTexture, i.uv ).r * 256;
	float4 rgba = GetPalette15Colour( PaletteIndex );
	return float4( rgba.xyz,1 );
}



float4 GetSpriteColourFromIndex(int TileIndex,float2 SpriteUv,int CharacterSet)
{
   //TileIndex *= 2;
   float4 TileColour = GetSpriteTileColour( TileIndex, SpriteUv, CharacterSet );
   return TileColour;
}
                                                                  
float4 GetSpriteColour(int4 Sprite,float2 SpriteUv,int CharacterSet)
{
	bool ColourMode256 = (Sprite[0] & (1 << 13)) != 0;
	//	if ((i & 0x1ff) < 240 && true)
	bool Mirror = (Sprite[1] & (1 << 12)) != 0;
	bool Flip = (Sprite[1] & (1 << 13)) != 0;
	bool DoubleScale = (Sprite[0] & (1 << 13)) != 0;

	//	https://www.coranac.com/tonc/text/regobj.htm#sec-tiles
	//bool TwoDimensionMapping = (this.dispCnt & (1 << 6)) == 0;
	bool TwoDimensionMapping = false;

	/*
	int baseSprite;
	if ((this.dispCnt & (1 << 6)) != 0)
	{
	// 1 dimensional
	baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
	}
	else
	{
	// 2 dimensional
	baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
	}
	int baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

    */

	//if ( Mirror )  	SpriteUv.x = 1-SpriteUv.x;
	//if ( Flip )	   	SpriteUv.y = 1-SpriteUv.y;

	float EdgeSize = 0.03f;
	bool RenderEdge = (SpriteUv.x < EdgeSize) || (SpriteUv.x > 1-EdgeSize) || (SpriteUv.y < EdgeSize) || (SpriteUv.y > 1-EdgeSize);

    //	grab tile for this chunk
    int2 TileOffset = float2(0,0);
    int2 SpriteSize = GetSpriteSize( Sprite );
    int2 SpriteSizeChunks = SpriteSize / int2(8,8);

    SpriteUv *= float2( SpriteSize.x, SpriteSize.y ) / 8.0f;

	//TileOffset = floor(SpriteUv);
	TileOffset = SpriteUv;

    SpriteUv = fmod( SpriteUv, 1 );

	int TileIndex = GetSpriteTileIndex( Sprite );

	int OffsetScale = DoubleScale ? 2 : 1;

	//	1D mapping
	if ( !TwoDimensionMapping )
	{
		if ( Mirror )
		{
			TileOffset.x = SpriteSizeChunks.x - TileOffset.x;
			//swap start
			//baseSprite += ((width / 8) * scale) - scale;
			//baseInc = -baseInc	//	work backwards

			//SpriteUv.x = 1-SpriteUv.x;
		}
		TileIndex += TileOffset.x * OffsetScale;
		TileIndex += (TileOffset.y * SpriteSizeChunks.x) * OffsetScale;
	}

	//if ( TileOffset.x > 0 || TileOffset.y > 0 )
	//	return float4(1,1,1,1);

	//return float4( SpriteUv, 0, 1 );
	float4 TileColour = GetSpriteTileColour( TileIndex, SpriteUv, CharacterSet );

	if ( RenderEdge )
		return float4(1,0,1,1);

    return TileColour;
}

