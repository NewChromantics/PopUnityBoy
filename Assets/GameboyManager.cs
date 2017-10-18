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
	}


	void UpdatePaletteTexture()
	{
		var BytesPerColour = 2;
		var PaletteComponents = this.Memory.PaletteRam;
		var PaletteColourCount = PaletteComponents.Length / BytesPerColour;

		if (PaletteTexture == null || PaletteTexture.width != PaletteColourCount)
			PaletteTexture = null;
		if (PaletteTexture == null) {
			PaletteTexture = new Texture2D (PaletteColourCount, 1, TextureFormat.RG16, false);
			PaletteTexture.filterMode = FilterMode.Point;
		}
		PaletteTexture.LoadRawTextureData (PaletteComponents);
		PaletteTexture.Apply ();
		OnPaletteTextureUpdated.Invoke(PaletteTexture);
	}
}
