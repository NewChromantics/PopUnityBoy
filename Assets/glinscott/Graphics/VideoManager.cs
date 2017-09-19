namespace GarboDev
{
    public class VideoManager
    {       
        Memory memory = null;
        int curLine;
		IRenderer Renderer;
		System.Action OnRenderFrame;
		System.Action OnPresent;

       

		public VideoManager(Memory memory,IRenderer Renderer,System.Action OnRenderFrame,System.Action OnPresent)
        {
			this.memory = memory;
			this.Renderer = Renderer;
			this.OnRenderFrame = OnRenderFrame;
        }

        public void Reset()
        {
            this.curLine = 0;

			Renderer.Reset ();
        }

        private void EnterVBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat |= 1;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            // Render the frame
            //this.gbaManager.FramesRendered++;
			OnRenderFrame.Invoke();
            this.Renderer.ShowFrame();

            if ((dispstat & (1 << 3)) != 0)
            {
                // Fire the vblank irq
                processor.RequestIrq(0);
            }

            // Check for DMA triggers
            this.memory.VBlankDma();
        }

        private void LeaveVBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat &= 0xFFFE;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            processor.UpdateKeyState();

            // Update the rot/scale values
            this.memory.Bgx[0] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG2X_L);
            this.memory.Bgx[1] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG3X_L);
            this.memory.Bgy[0] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG2Y_L);
            this.memory.Bgy[1] = (int)Memory.ReadU32(this.memory.IORam, Memory.BG3Y_L);
        }

        public void EnterHBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat |= 1 << 1;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            // Advance the bgx registers
            for (int bg = 0; bg <= 1; bg++)
            {
                short dmx = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PB + (uint)bg * 0x10);
                short dmy = (short)Memory.ReadU16(this.memory.IORam, Memory.BG2PD + (uint)bg * 0x10);
                this.memory.Bgx[bg] += dmx;
                this.memory.Bgy[bg] += dmy;
            }

            if (this.curLine < 160)
            {
                this.memory.HBlankDma();

                // Trigger hblank irq
                if ((dispstat & (1 << 4)) != 0)
                {
                    processor.RequestIrq(1);
                }
            }
        }

        public void LeaveHBlank(Arm7Processor processor)
        {
            ushort dispstat = Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT);
            dispstat &= 0xFFF9;
            Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

            // Move to the next line
            this.curLine++;

            if (this.curLine >= 228)
            {
                // Start again at the beginning
                this.curLine = 0;
            }

            // Update registers
            Memory.WriteU16(this.memory.IORam, Memory.VCOUNT, (ushort)this.curLine);

            // Check for vblank
            if (this.curLine == 160)
            {
                this.EnterVBlank(processor);
            }
            else if (this.curLine == 0)
            {
                this.LeaveVBlank(processor);
            }

            // Check y-line trigger
            if (((dispstat >> 8) & 0xff) == this.curLine)
            {
                dispstat = (ushort)(Memory.ReadU16(this.memory.IORam, Memory.DISPSTAT) | (1 << 2));
                Memory.WriteU16(this.memory.IORam, Memory.DISPSTAT, dispstat);

                if ((dispstat & (1 << 5)) != 0)
                {
                    processor.RequestIrq(2);
                }
            }
        }

        public void RenderLine()
        {
            if (this.curLine < 160)
            {
				Renderer.RenderLine(this.curLine);
            }
        }
    }
}