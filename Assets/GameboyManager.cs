using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;

[System.Serializable]
public class UnityEvent_Texture : UnityEvent <Texture> {}


public class GameboyManager : MonoBehaviour {

	public TextAsset		Bios;
	public TextAsset		Rom;
	GarboDev.GbaManager		GbaManager;
	GarboDev.IRenderer		Renderer;
	GarboDev.Memory			Memory;
	public RenderTexture	FrameTarget;
	Texture2D				Frame2D;
	public UnityEvent_Texture	OnTextureUpdated;
	public bool				Flip = true;
	public bool				ForceOpaque = true;

	[Range(0,10)]
	public float			TimeScalar = 1;
	float?					StartTime = null;

	[Range(100,160*240)]
	public int				WritePixelsPerThread = 100;
	[Range(1,160*240)]
	public int				PixelCountClip = 160*240;


	//	renderer stuff
	public UnityEvent_Texture	OnPaletteTextureUpdated;
	Texture2D					PaletteTexture;

	public UnityEvent_Texture	OnVRamTextureUpdated;
	Texture2D					VRamTexture;

	public UnityEvent_Texture	OnIoRamTextureUpdated;
	Texture2D					IoRamTexture;

	public UnityEvent_Texture	OnOamRamTextureUpdated;
	Texture2D					OamRamTexture;

	public bool					DebugPalette = false;
	[Range(0,31)]
	public int					DebugPaletteRed = 31;
	[Range(0,31)]
	public int					DebugPaletteGreen = 31;
	[Range(0,31)]
	public int					DebugPaletteBlue = 0;
	[Range(0,1)]
	public int					DebugPaletteAlpha = 0;

	void Start ()
	{
		if ( FrameTarget != null )
			Graphics.Blit (Texture2D.whiteTexture, FrameTarget);

		Memory = new GarboDev.Memory ();

		GbaManager = new GarboDev.GbaManager (Memory,GetSystemTimeSecs,ResetTime);

		Frame2D = new Texture2D (240,160, TextureFormat.ARGB32, false);

		Renderer = new GarboDev.Renderer (Memory);

		GbaManager.LoadBios (Bios.bytes);

		//	pad rom to power2 
		var RomBytes = Rom.bytes;
		if (!Mathf.IsPowerOfTwo (RomBytes.Length)) {
			RomBytes = new byte[ Mathf.NextPowerOfTwo (RomBytes.Length) ];
			Rom.bytes.CopyTo (RomBytes, 0);
		}

		GbaManager.StartEmulator ( Renderer );
		GbaManager.LoadRom (RomBytes);
		GbaManager.Resume ();
	}

	float GetSystemTimeSecs()
	{
		if (!StartTime.HasValue)
			StartTime = Time.time;

		var TimeElapsed = Time.time - StartTime.Value;
		TimeElapsed *= TimeScalar;
		return TimeElapsed;
	}

	void ResetTime ()
	{
		StartTime = null;
	}


	unsafe void ThreadedCopy(uint* Frame,Color* Pixels,int Frame2D_width,int Frame2D_height,int FrameArray_Length,int PixelsArray_Length)
	{
		int PixelsDrawn = 0;
		int PixelsQueued = 0;

		bool Aborted = false;
		System.Action<int,int> CopyPixels = (PixelIndex, PixelCount) => 
		{
			try
			{
				var rgba = new Color ();

				for (int p = PixelIndex;	p <PixelIndex+PixelCount;	p++) {

					var x = p % Frame2D_width;
					var y = p / Frame2D_width;

					if (Flip)
						y = Frame2D_height -1 - y;

					var FrameIndex = x + (y * Frame2D_width);
					var rgba32 = Frame [FrameIndex];

					var a = (rgba32 >> 24) & 0xff;
					var r = (rgba32 >> 16) & 0xff;
					var g = (rgba32 >> 8) & 0xff;
					var b = (rgba32 >> 0) & 0xff;

					if ( ForceOpaque )
						a = 255;

					rgba.r = r / 255.0f;
					rgba.g = g / 255.0f;
					rgba.b = b / 255.0f;
					rgba.a = a / 255.0f;
					Pixels [p] = rgba;
				}
			}
			catch
			{
				Aborted = true;
			}

			Interlocked.Add( ref PixelsDrawn, PixelCount );
		};


		var TotalPixels = Mathf.Min (PixelCountClip, Mathf.Min (FrameArray_Length, PixelsArray_Length));
		int ThreadCount = TotalPixels / WritePixelsPerThread;
		for (int t = 0;	t < ThreadCount+1;	t++) {
			int FirstPixel = WritePixelsPerThread * t;
			int PixelsToDraw = WritePixelsPerThread;

			if (FirstPixel + PixelsToDraw > TotalPixels)
				PixelsToDraw = TotalPixels - FirstPixel;

			PixelsQueued += PixelsToDraw;

			if ( PixelsToDraw > 0 )
				ThreadPool.QueueUserWorkItem ( (x) =>	{	CopyPixels (FirstPixel, PixelsToDraw);	}	);
		}

		while (PixelsDrawn < PixelsQueued && !Aborted ) {
			//Debug.Log ("Waiting for " + (PixelsQueued - PixelsDrawn) + " pixels to draw");
			Thread.Sleep(1);
		}
	}
		



	void Update () 
	{
		//	overwrite palette
		if ( DebugPalette )
		{
			int Colour16 = 0;
			Colour16 |= DebugPaletteRed;
			Colour16 |= DebugPaletteGreen << 5;
			Colour16 |= DebugPaletteBlue << 10;
			Colour16 |= DebugPaletteAlpha << 15;
			byte Colour8a = (byte)( (Colour16>>0) & 0xff);
			byte Colour8b = (byte)( (Colour16>>8) & 0xff );
			for (int i = 0;	i < Memory.palRam.Length;	i += 2) {
				Memory.palRam [i + 0] = Colour8a;
				Memory.palRam [i + 1] = Colour8b;
			}
		}

		GbaManager.EmulatorIteration ();

		var renderer = Renderer as GarboDev.Renderer;
		var PixelsArray = Frame2D.GetPixels ();
		var FrameArray = renderer.GetFrame ();

		unsafe
		{
			fixed(uint* Frame = &FrameArray[0])
			{
				fixed(Color* Pixels = &PixelsArray[0])
				{
					ThreadedCopy( Frame, Pixels, Frame2D.width, Frame2D.height, FrameArray.Length, PixelsArray.Length );
				}
			}
		}

		Frame2D.SetPixels (PixelsArray);
		Frame2D.Apply ();

		if ( FrameTarget != null )
			Graphics.Blit (Frame2D, FrameTarget);

		OnTextureUpdated.Invoke (Frame2D);

		UpdatePaletteTexture ();
		UpdateVRamTexture ();
		UpdateIoRamTexture ();
		UpdateOamRamTexture ();
	}

	enum ComponentSize
	{
		Eight=1,
		Sixteen=2,
	}

	void UpdatePaletteTexture()
	{
		UpdateTexture (ref PaletteTexture, OnPaletteTextureUpdated, this.Memory.PaletteRam, ComponentSize.Sixteen, 32 );
	}


	void UpdateVRamTexture()
	{
		UpdateTexture (ref VRamTexture, OnVRamTextureUpdated, this.Memory.VideoRam, ComponentSize.Eight, 256 );
	}

	void UpdateIoRamTexture()
	{
		UpdateTexture (ref IoRamTexture, OnIoRamTextureUpdated, this.Memory.IORam, ComponentSize.Eight, 32 );
	}

	void UpdateOamRamTexture()
	{
		UpdateTexture (ref OamRamTexture, OnOamRamTextureUpdated, this.Memory.OamRam, ComponentSize.Eight, 128 );
	}

	static void UpdateTexture(ref Texture2D RamTexture,UnityEvent_Texture Event,byte[] Ram,ComponentSize Size,int Width=256)
	{
		var DataLength = Ram.Length / (int)Size;
		var Height = DataLength / Width;

		if (!Mathf.IsPowerOfTwo (Height))
			Height = Mathf.NextPowerOfTwo (Height);

		if (RamTexture == null || RamTexture.width != Width || RamTexture.height != Height)
			RamTexture = null;
		if (RamTexture == null) 
		{
			var Format = Size == ComponentSize.Eight ? TextureFormat.R8 : TextureFormat.RG16;
			RamTexture = new Texture2D (Width, Height, Format, false);
			RamTexture.filterMode = FilterMode.Point;
			RamTexture.wrapMode = TextureWrapMode.Clamp;
		}

		//	gr: might need to realign these bytes
		var RawPixels = RamTexture.GetRawTextureData();
		if (RawPixels.Length != Ram.Length) 
		{
			Ram.CopyTo (RawPixels, 0);
			RamTexture.LoadRawTextureData (RawPixels);
		} 
		else
		{
			RamTexture.LoadRawTextureData (Ram);
		}

		RamTexture.Apply ();
		Event.Invoke(RamTexture);
	}
}
