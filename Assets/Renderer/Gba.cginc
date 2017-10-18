#if !defined(VRamTexture)
sampler2D VRamTexture;
float4 VRamTexture_TexelSize;
#endif

#if !defined(IoRamTexture)
sampler2D IoRamTexture;
float4 IoRamTexture_TexelSize;
#endif

#if !defined(PaletteTexture)
sampler2D PaletteTexture;
float4 PaletteTexture_TexelSize;
#endif


float RightShift(float f,int ShiftAmount)	
{
	return floor(f / pow(2,ShiftAmount) );	
}

float LeftShift(float f,int ShiftAmount)	
{
	return f * pow(2,ShiftAmount);	
}

float And3(float Value)
{
	Value = fmod( Value, 3 );
	return Value;
}

//	adapted from https://stackoverflow.com/a/27551433
float4 UnpackColor15(float f) 
{
	float a = RightShift( f, 15 );
	float r = RightShift( f - LeftShift(a,15), 10 );
	float g = RightShift( f - LeftShift(r,10), 5 );
	float b = floor( f - LeftShift(r,10) - LeftShift(g,5) );

	//	normalize
	float Max5 = pow( 2,5 );
	float4 rgba = float4( r,g,b,a ) / float4( Max5, Max5, Max5, 1 );
	rgba = clamp( 0, 1, rgba );

	//	gr: alpha is always 0?
	a = 1;

	return rgba;
}

float4 GetPalette15Colour(int Index)
{
	int SourceWidth = PaletteTexture_TexelSize.z;
	int SourceHeight = PaletteTexture_TexelSize.w;
	int Samplex = Index % SourceWidth;
	int Sampley = 1;
	float2 Sampleuv = float2(Samplex,Sampley) / float2(SourceWidth,SourceHeight);

	//	scale float data to 16 bit
	float2 PaletteColour8_8 = tex2D(PaletteTexture, Sampleuv).xy * float2( 256, 256 );
	float PaletteColour16 = PaletteColour8_8.x + (PaletteColour8_8.y*256);

	float4 rgba = UnpackColor15( PaletteColour16 ).bgra;
	return rgba;
}

int GetVRam8(int Index)
{
	int w = VRamTexture_TexelSize.z;
	int h = VRamTexture_TexelSize.w;
	float u = (Index % w) / w;
	float v = (Index / w) / h;

	float Value = tex2D( VRamTexture, float2(u,v) ) * 256;

	return Value;
}


int GetIoRam8(int Index)
{
	int w = IoRamTexture_TexelSize.z;
	int h = IoRamTexture_TexelSize.w;
	float u = (Index % w) / w;
	float v = (Index / w) / h;

	float Value = tex2D( IoRamTexture, float2(u,v) ) * 256;

	return Value;
}

int GetIoRam16(int Index)
{
	int x = GetIoRam8( Index );
	x += GetIoRam8( Index+1 ) * 256;
	return x;
}

//	mode 1 stuff
float4 GetBgColour()
{
	return GetPalette15Colour( 0 );
}

