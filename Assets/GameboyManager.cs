using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;

[System.Serializable]
public class UnityEvent_Texture : UnityEvent <Texture> {}


public class GameboyManager : MonoBehaviour {

	const int GbaScreenWidth = 240;
	const int GbaScreenHeight = 160;

	public TextAsset		Bios;
	public TextAsset		Rom;
	GarboDev.GbaManager		GbaManager;
	GarboDev.IRenderer		Renderer;
	GarboDev.Memory			Memory;
	public bool				EnableCpuRenderer = true;
	public RenderTexture	FrameTarget;
	Texture2D				Frame2D;

	public Material			FrameRenderer;
	public bool				BlitFrameInCoRoutine = true;
	RenderTexture			RenderedFrame;


	public UnityEvent_Texture	OnTextureUpdated;
	public bool				Flip = true;
	public bool				ForceOpaque = true;

	[Range(0,10)]
	public float			TimeScalar = 1;
	float?					StartTime = null;

	[Range(100,160*240)]
	public int				WritePixelsPerThread = 100;
	[Range(1,160*240)]
	public int				PixelCountClip = GbaScreenWidth*GbaScreenHeight;


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


	public string				InputAxisHorizontal = "Horizontal";
	public string				InputAxisVertical = "Vertical";
	public string				InputActionA = "Fire1";
	public string				InputActionB = "Fire2";
	public string				InputActionL = "Fire3";
	public string				InputActionR = "Jump";
	public string				InputActionStart = "Submit";
	public string				InputActionSelect = "Cancel";

	void Start ()
	{
		if ( FrameTarget != null )
			Graphics.Blit (Texture2D.whiteTexture, FrameTarget);

		Memory = new GarboDev.Memory ();

		GbaManager = new GarboDev.GbaManager (Memory,GetSystemTimeSecs,ResetTime);

		Frame2D = new Texture2D (GbaScreenWidth,GbaScreenHeight, TextureFormat.ARGB32, false);

		if ( EnableCpuRenderer )
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

	IEnumerator BlitFrameCoroutine()
	{
		yield return new WaitForEndOfFrame();
		BlitFrame ();
	}

	void QueueBlitFrame()
	{
		if (BlitFrameInCoRoutine)
			StartCoroutine ( BlitFrameCoroutine() );
		else
			BlitFrame ();
	}

	void BlitFrame()
	{
		Graphics.Blit (null, RenderedFrame, FrameRenderer);
		OnTextureUpdated.Invoke (RenderedFrame);
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

		UpdateInput ();

		GbaManager.EmulatorIteration ();

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

		UpdatePaletteTexture ();
		UpdateVRamTexture ();
		UpdateIoRamTexture ();
		UpdateOamRamTexture ();

		if (FrameRenderer != null) {
			if (RenderedFrame == null) {
				RenderedFrame = new RenderTexture (GbaScreenWidth, GbaScreenHeight, 0 );
				RenderedFrame.useMipMap = false;
				RenderedFrame.filterMode = FilterMode.Point;
			}
			QueueBlitFrame();
		}
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

	static Dictionary<Texture,byte[]>	TextureAlignedByteCache;

	static byte[] GetTextureSizedBytes(Texture2D Texture,byte[] Data)
	{
		var TextureSize = (Texture.format == TextureFormat.R8) ?  1 : 2;
		TextureSize *= Texture.width;
		TextureSize *= Texture.height;

		if (Data.Length == TextureSize)
			return Data;

		if (TextureAlignedByteCache == null)
			TextureAlignedByteCache = new Dictionary<Texture,byte[]> ();

		if (!TextureAlignedByteCache.ContainsKey (Texture)) {
			var NewData = new byte[TextureSize];
			TextureAlignedByteCache [Texture] = NewData;
		}

		var ResizedData = TextureAlignedByteCache[Texture];
		Data.CopyTo (ResizedData, 0);
		return ResizedData;
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

		var PixelData = GetTextureSizedBytes (RamTexture, Ram);
		RamTexture.LoadRawTextureData (PixelData);
	
		RamTexture.Apply ();
		Event.Invoke(RamTexture);
	}


	void UpdateInput()
	{
		var LeftRight = Input.GetAxis (InputAxisHorizontal);
		var Left = (LeftRight < 0);
		var Right = (LeftRight > 0);
		var UpDown = Input.GetAxis (InputAxisVertical);
		var Down = (UpDown < 0);
		var Up = (UpDown > 0);
		var A = Input.GetButton (InputActionA);
		var B = Input.GetButton (InputActionB);
		var Start = Input.GetButton (InputActionStart);
		var Select = Input.GetButton (InputActionSelect);
		var L = Input.GetButton (InputActionL);
		var R = Input.GetButton (InputActionR);

		int State = 0;
		State |= (A ? 1 : 0) << 0;
		State |= (B ? 1 : 0) << 1;
		State |= (Select ? 1 : 0) << 2;
		State |= (Start ? 1 : 0) << 3;
		State |= (Right ? 1 : 0) << 4;
		State |= (Left ? 1 : 0) << 5;
		State |= (Up ? 1 : 0) << 6;
		State |= (Down ? 1 : 0) << 7;
		State |= (R ? 1 : 0) << 8;
		State |= (L ? 1 : 0) << 9;

		//	state is inverted
		ushort StateMask = (ushort)((1<<10)-1);
		var State16 = (ushort)((~State) & StateMask);

		GbaManager.KeyState = State16;
	}

}
