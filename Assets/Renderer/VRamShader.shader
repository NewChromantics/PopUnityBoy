Shader "NewChromantics/PopUnityBoy/VRamShader"
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


			#include "UnityCG.cginc"

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


			float RightShift(float f,int ShiftAmount)	{	return floor(f / pow(2,ShiftAmount) );	}
			float LeftShift(float f,int ShiftAmount)	{	return f * pow(2,ShiftAmount);	}

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
			    return rgba;
			}

			float4 GetPalette15Colour(int Index)
			{
				int SourceWidth = _MainTex_TexelSize.z;
				int SourceHeight = _MainTex_TexelSize.w;
				int Samplex = Index % SourceWidth;
				int Sampley = 1;
				float2 Sampleuv = float2(Samplex,Sampley) / float2(SourceWidth,SourceHeight);

				//	scale float data to 16 bit
				float2 PaletteColour8_8 = tex2D(_MainTex, Sampleuv).xy * float2( 256, 256 );
				float PaletteColour16 = PaletteColour8_8.x + (PaletteColour8_8.y*256);

				float4 rgba = UnpackColor15( PaletteColour16 ).bgra;
				return rgba;
			}


			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}
