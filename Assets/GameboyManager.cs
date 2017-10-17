using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameboyManager : MonoBehaviour {

	public TextAsset		Bios;
	public TextAsset		Rom;
	GarboDev.GbaManager		GbaManager;
	GarboDev.IRenderer		Renderer;
	GarboDev.Memory			Memory;
	public RenderTexture	FrameTarget;
	Texture2D				Frame2D;
	public bool				Flip = true;
	public bool				ForceOpaque = true;

	[Range(0,10)]
	public float			TimeScalar = 1;
	float?					StartTime = null;

	void Start ()
	{
		Debug.Log("Start");
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
		Debug.Log("Start finished");
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

	void Update () 
	{
		if (GbaManager == null)
			Debug.Log ("Gbamanager is null");
		
		GbaManager.EmulatorIteration ();

		var renderer = Renderer as GarboDev.Renderer;
		var Pixels = Frame2D.GetPixels ();
		var Frame = renderer.GetFrame ();

		{
			var rgba = new Color ();
			for (int p = 0;	p < Mathf.Min (Frame.Length, Pixels.Length);	p++) {

				var x = p % Frame2D.width;
				var y = p / Frame2D.width;

				if (Flip)
					y = Frame2D.height -1 - y;

				var FrameIndex = x + (y * Frame2D.width);
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
		Frame2D.SetPixels (Pixels);
		Frame2D.Apply ();
		Graphics.Blit (Frame2D, FrameTarget);

	}
}
