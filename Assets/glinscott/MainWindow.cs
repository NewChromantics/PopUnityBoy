namespace GarboDev
{
    using System;
    using System.Collections.Generic;
	/*
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.DirectX;
    using Microsoft.DirectX.Direct3D;
*/
    partial class MainWindow
    {
		System.Action<string>	Debug;
		/*
        private DissassemblyWindow disassembly = null;
        private PaletteWindow palette = null;
        private SpriteWindow sprites = null;
*/
        private GbaManager gbaManager = null;
		/*   private SoundPlayer soundPlayer = null;
	
        private Device device = null;

        private VertexBuffer screenQuad = null;
        private Texture backgroundTexture = null;
*/
        private enum RendererType
        {
            GDIRenderer,
            D3DRenderer,
            ShaderRenderer
        }

        private RendererType rendererType;

        private int width, height;

		public MainWindow()
        {
           // this.gbaManager = new GbaManager();
			/*
            // Initialize the sound subsystem
            if (this.EnableSound.Checked)
            {
                this.soundPlayer = new SoundPlayer(this, this.gbaManager.AudioMixer, 2);
                this.soundPlayer.Resume();
            }

            this.width = 240 * 2;
            this.height = 160 * 2;

            Timer timer = new Timer();
            timer.Interval = 50;
            timer.Tick += new EventHandler(UpdateFps);
            timer.Enabled = true;

            this.biosFilename = "C:\\Documents and Settings\\Administrator\\My Documents\\Visual Studio\\Projects\\GarboDev\\gbabios.bin";

            if (this.OptionsUseBios.Checked)
            {
                this.LoadBios();
            }

            this.OptionsSkipBios.Checked = this.gbaManager.SkipBios;
            this.OptionsLimitFps.Checked = this.gbaManager.LimitFps;
			*/
        }

		private void LoadBios(byte[] rom)
        {
            try
            {
          		this.gbaManager.LoadBios(rom);
            }
            catch (Exception exception)
            {
                Debug("Unable to load bios file, disabling bios (irq's will not work)\n" + exception.Message);

                //this.OptionsBiosFile.Checked = false;
            }
        }
		/*
        private void UpdateFps(object sender, EventArgs e)
        {
            if (this.gbaManager == null) return;

            int t1 = this.gbaManager.FramesRendered;
            double t2 = this.gbaManager.SecondsSinceStarted;

            this.framesRendered.Enqueue(t1);
            this.secondsSinceStarted.Enqueue(t2);

            int frameDiff = t1 - this.framesRendered.Peek();
            double timeDiff = t2 - this.secondsSinceStarted.Peek();

            if (this.framesRendered.Count > 10)
            {
                this.framesRendered.Dequeue();
                this.secondsSinceStarted.Dequeue();
            }

            this.StatusStrip.Items[0].Text = "Fps: " + string.Format("{0,2:f}", frameDiff / timeDiff);
        }
	
        protected override void OnResizeBegin(EventArgs e)
        {
            this.Halt();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            if (!this.FilePause.Checked)
            {
                this.Resume();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            this.width = this.ClientSize.Width;
            this.height = this.ClientSize.Height - this.MainMenu.Height - this.StatusStrip.Height;

            if (this.screenQuad != null)
            {
                this.OnScreenQuadCreated(this.screenQuad, null);
            }
        }
*/
		/*
        private void InitializeD3D()
        {
            try
            {
                PresentParameters presentParams = new PresentParameters();
                presentParams.Windowed = true;
                presentParams.SwapEffect = SwapEffect.Copy;

                if (!this.OptionsVsync.Checked)
                {
                    presentParams.PresentationInterval = PresentInterval.Immediate;
                }

                this.device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing,
                    presentParams);
            }
            catch (DirectXException exception)
            {
                MessageBox.Show("Unable to create Direct3D instance (perhaps you need to install the managed directx client?)\n" + exception.Message);
                throw exception;
            }

            this.screenQuad = new VertexBuffer(typeof(CustomVertex.TransformedColoredTextured),
                4, this.device, Usage.WriteOnly, CustomVertex.TransformedColoredTextured.Format, Pool.Default);
            this.screenQuad.Created += new EventHandler(OnScreenQuadCreated);
            this.OnScreenQuadCreated(this.screenQuad, null);

            if (this.rendererType == RendererType.D3DRenderer)
            {
                this.backgroundTexture = new Texture(this.device, 240, 160, 1, Usage.None,
                    Format.X8R8G8B8, Pool.Managed);
            }
        }
*/
		/*
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys keys);

        public void CheckKeysHit()
        {
            Keys[] keymap = new Keys[]
                {
                    Keys.A,
                    Keys.B,
                    Keys.RShiftKey,
                    Keys.Enter,
                    Keys.NumPad6,
                    Keys.NumPad4,
                    Keys.NumPad8,
                    Keys.NumPad2,
                    Keys.R,
                    Keys.L
                };

            ushort keyreg = 0x3FF;

            for (int i = 0; i < keymap.Length; i++)
            {
                if (GetAsyncKeyState(keymap[i]) < 0)
                {
                    keyreg &= (ushort)(~(1U << i));
                }
                else
                {
                    keyreg |= (ushort)(1U << i);
                }
            }

            this.gbaManager.KeyState = keyreg;
        }
        */


        private void RenderShader(object data)
        {
			/*
            if (this.device == null)
                return;

            this.device.Clear(ClearFlags.Target, Color.Black, 0.0f, 0);

            this.device.BeginScene();

            this.device.SetTexture(0, data as Texture);
            this.device.TextureState[0].ColorOperation = TextureOperation.Modulate;
            this.device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
            this.device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
            this.device.TextureState[0].AlphaOperation = TextureOperation.Disable;

            this.device.RenderState.CullMode = Cull.None;
            this.device.RenderState.Lighting = false;
            this.device.RenderState.ZBufferEnable = false;

            this.device.SetStreamSource(0, this.screenQuad, 0);
            this.device.VertexFormat = CustomVertex.TransformedColoredTextured.Format;
            this.device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

            this.device.SetStreamSource(0, null, 0);

            this.device.EndScene();
            this.device.Present();
            */
        }


        private Queue<int> framesRendered = new Queue<int>();
        private Queue<double> secondsSinceStarted = new Queue<double>();

		public void LoadRom(byte[] rom)
		{
		/*
        private void FileOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "bin files (*.bin;*.gba)|*.bin;*.gba|All files (*.*)|*.*";
            dialog.FilterIndex = 0;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (Stream stream = dialog.OpenFile())
                {
                    int romSize = 1;
                    while (romSize < stream.Length)
                    {
                        romSize <<= 1;
                    }

                    byte[] rom = new byte[romSize];
                    stream.Read(rom, 0, (int)stream.Length);

                    this.gbaManager.LoadRom(rom);
                }

                if (!this.FilePause.Checked)
                {
                    this.Resume();
                }
            }
        }
        */
			this.gbaManager.LoadRom(rom);
		}
		/*
        private void FileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.gbaManager != null)
            {
                e.Cancel = true;

                this.Shutdown();

                this.Close();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!this.FilePause.Checked)
            {
                this.Resume();
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            if (this.gbaManager != null)
            {
                this.Halt();
            }
        }

        private void DebugDissassembly_Click(object sender, EventArgs e)
        {
            this.disassembly = new DissassemblyWindow(this.gbaManager);
            this.disassembly.Show();

            this.disassembly.Location = new Point(this.Location.X + this.Width + 15, this.Location.Y);
        }

        private void DebugPalette_Click(object sender, EventArgs e)
        {
            this.palette = new PaletteWindow(this.gbaManager);
            this.palette.Show();
        }

        private void DebugSprites_Click(object sender, EventArgs e)
        {
            this.sprites = new SpriteWindow(this.gbaManager);
            this.sprites.Show();
        }
		
        private void OnScreenQuadCreated(object sender, EventArgs e)
        {
            VertexBuffer vb = (VertexBuffer)sender;
            CustomVertex.TransformedColoredTextured[] verts = (CustomVertex.TransformedColoredTextured[])vb.Lock(0, 0);

            float topx = 0, topy = this.MainMenu.Height;

            verts[0].Position = new Vector4(topx - 0.5f, topy - 0.5f, 0, 1);
            verts[0].Color = Color.White.ToArgb();
            verts[0].Tu = 0; verts[0].Tv = 0;
            verts[1].Position = new Vector4(topx + this.width - 0.5f, topy - 0.5f, 0, 1);
            verts[1].Color = Color.White.ToArgb();
            verts[1].Tu = 1; verts[1].Tv = 0;
            verts[2].Position = new Vector4(topx + this.width - 0.5f, topy + this.height - 0.5f, 0, 1);
            verts[2].Color = Color.White.ToArgb();
            verts[2].Tu = 1; verts[2].Tv = 1;
            verts[3].Position = new Vector4(topx - 0.5f, topy + this.height - 0.5f, 0, 1);
            verts[3].Color = Color.White.ToArgb();
            verts[3].Tu = 0; verts[3].Tv = 1;

            vb.Unlock();
        }

        private void FilePause_Click(object sender, EventArgs e)
        {
            if (!this.FilePause.Checked)
            {
                this.Resume();
            }
            else
            {
                this.Halt();
                this.device.Present();
            }
        }

        private void FileReset_Click(object sender, EventArgs e)
        {
            this.soundPlayer.Pause();
            this.gbaManager.Reset();

            if (!this.FilePause.Checked)
            {
                this.Resume();
            }
        }

        private void OptionsBiosFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "bin files (*.bin;*.gba)|*.bin;*.gba|All files (*.*)|*.*";
            dialog.FilterIndex = 0;
            dialog.FileName = this.biosFilename;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.biosFilename = dialog.FileName;
                this.LoadBios();
            }
        }

        private void OptionsSkipBios_Click(object sender, EventArgs e)
        {
            this.gbaManager.SkipBios = this.OptionsSkipBios.Checked;
        }

        private void OptionsVsync_Click(object sender, EventArgs e)
        {
            if (this.device != null)
            {
                PresentParameters parameters = this.device.PresentationParameters;

                if (this.OptionsVsync.Checked)
                {
                    parameters.PresentationInterval = PresentInterval.Default;
                }
                else
                {
                    parameters.PresentationInterval = PresentInterval.Immediate;
                }

                this.Halt();
                this.device.Reset(parameters);
                this.Resume();
            }
        }

        private void OptionsLimitFps_Click(object sender, EventArgs e)
        {
            this.gbaManager.LimitFps = this.OptionsLimitFps.Checked;
        }

        private void OptionsRenderersD3D_Click(object sender, EventArgs e)
        {
            this.SetRenderType(RendererType.D3DRenderer);
        }

        private void OptionsRenderersShader_Click(object sender, EventArgs e)
        {
            this.SetRenderType(RendererType.ShaderRenderer);
        }

        private void OptionsRenderersGDI_Click(object sender, EventArgs e)
        {
            this.SetRenderType(RendererType.GDIRenderer);
        }

        private void OptionsSizex1_Click(object sender, EventArgs e)
        {
            this.ResizeWindow(1, 1);
        }

        private void OptionsSizex2_Click(object sender, EventArgs e)
        {
            this.ResizeWindow(2, 2);
        }

        private void OptionsSizex3_Click(object sender, EventArgs e)
        {
            this.ResizeWindow(3, 3);
        }

        private void ResizeWindow(int xScale, int yScale)
        {
            this.width = 240 * xScale;
            this.height = 160 * yScale;

            this.Halt();

            this.ClientSize = new Size(this.width, this.height + this.MainMenu.Height + this.StatusStrip.Height);

            if (!this.FilePause.Checked)
            {
                this.Resume();
            }
        }

        private void Halt()
        {
            this.soundPlayer.Pause();
            this.gbaManager.Halt();
        }

        private void Resume()
        {
            this.gbaManager.Resume();
            if (this.EnableSound.Checked)
            {
                this.soundPlayer.Resume();
            }
        }

        private void enableSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.EnableSound.Checked = !this.EnableSound.Checked;
            if (!this.EnableSound.Checked)
            {
                this.soundPlayer.Pause();
            }
            else
            {
                this.soundPlayer.Resume();
            }
        }
        */
    }
    
}