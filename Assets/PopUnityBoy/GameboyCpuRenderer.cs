using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;



public class GameboyCpuRenderer : MonoBehaviour {

	const int GbaScreenWidth = 240;
	const int GbaScreenHeight = 160;

	public GarboDev.IRenderer	Renderer;

	GarboDev.Memory			Memory				{	get	{ return GetComponent<GameboyManager> ().Memory; }}

	public RenderTexture	FrameTarget;
	Texture2D				Frame2D;


	public UnityEvent_Texture	OnTextureUpdated;
	public bool				Flip = true;
	public bool				ForceOpaque = true;

	[Range(100,160*240)]
	public int				WritePixelsPerThread = 100;
	[Range(1,160*240)]
	public int				PixelCountClip = GbaScreenWidth*GbaScreenHeight;



	void Start ()
	{
		if ( FrameTarget != null )
			Graphics.Blit (Texture2D.whiteTexture, FrameTarget);

		Renderer = new GarboDev.Renderer (Memory);

		Frame2D = new Texture2D (GbaScreenWidth,GbaScreenHeight, TextureFormat.ARGB32, false);

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
		try
		{
			var renderer = Renderer as GarboDev.Renderer;
			var FrameArray = renderer.GetFrame ();
			var PixelsArray = Frame2D.GetPixels ();

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
		}
		catch
		{
		}


	}





}
