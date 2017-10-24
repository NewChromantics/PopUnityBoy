using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class UnityEvent_Texture : UnityEvent <Texture> {}



[System.Serializable]
public class TPoke
{
	[System.Serializable]
	public enum MemoryRegion
	{
		Sram = 0x0E000000,
	};

	public MemoryRegion	Region = MemoryRegion.Sram;
	public int			Address;
	public byte			Value;
};

public class GameboyManager : MonoBehaviour {

	const int GbaScreenWidth = 240;
	const int GbaScreenHeight = 160;


	public TextAsset		Bios;
	public TextAsset		Rom;
	GarboDev.GbaManager		GbaManager;
	public GarboDev.IRenderer	Renderer	
	{	get	
		{
			try
			{
				return GetComponent<GameboyCpuRenderer>().Renderer;
			}
			catch
			{
				return null;
			}
		}
	}
	public GarboDev.Memory	Memory;


	[Range(0,10)]
	public float			TimeScalar = 1;
	float?					StartTime = null;



	public string				InputAxisHorizontal = "Horizontal";
	public string				InputAxisVertical = "Vertical";
	public string				InputActionA = "Fire1";
	public string				InputActionB = "Fire2";
	public string				InputActionL = "Fire3";
	public string				InputActionR = "Jump";
	public string				InputActionStart = "Submit";
	public string				InputActionSelect = "Cancel";

	public List<TPoke>			MemoryPokes;

	void Start ()
	{
		Memory = new GarboDev.Memory ();

		GbaManager = new GarboDev.GbaManager (Memory,GetSystemTimeSecs,ResetTime);

		GbaManager.LoadBios (Bios.bytes);

		//	pad rom to power2 
		var RomBytes = Rom.bytes;
		if (!Mathf.IsPowerOfTwo (RomBytes.Length)) {
			RomBytes = new byte[ Mathf.NextPowerOfTwo (RomBytes.Length) ];
			Rom.bytes.CopyTo (RomBytes, 0);
		}

		GbaManager.StartEmulator ( Renderer );
		GbaManager.LoadRom (RomBytes);

		if (MemoryPokes != null) {
			foreach (var Poke in MemoryPokes) {
				//	write to memory
				var Address = Poke.Region + Poke.Address;
				Memory.WriteU8 ( (uint)Address, Poke.Value);
			}
		}

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



	void Update () 
	{
		UpdateInput ();

		GbaManager.EmulatorIteration ();


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
