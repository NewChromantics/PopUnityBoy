using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif




public class GameboyGpuRenderer : MonoBehaviour {

	const int GbaScreenWidth = 240;
	const int GbaScreenHeight = 160;

	GarboDev.IRenderer		Renderer			{	get	{ return GetComponent<GameboyManager> ().Renderer; }}
	GarboDev.Memory			Memory				{	get	{ return GetComponent<GameboyManager> ().Memory; }}
	public bool				EnableCpuRenderer = true;

	public Material			FrameRenderer;
	public bool				BlitFrameInCoRoutine = true;
	RenderTexture			RenderedFrame;


	public UnityEvent_Texture	OnTextureUpdated;


	//	renderer stuff
	public UnityEvent_Texture	OnPaletteTextureUpdated;
	Texture2D					PaletteTexture;

	public UnityEvent_Texture	OnVRamTextureUpdated;
	Texture2D					VRamTexture;

	public UnityEvent_Texture	OnIoRamTextureUpdated;
	Texture2D					IoRamTexture;

	public UnityEvent_Texture	OnOamRamTextureUpdated;
	Texture2D					OamRamTexture;



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


}
