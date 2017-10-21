namespace GarboDev
{
    using System;

    public interface IRenderer
    {
        void Initialize(object data);
        void Reset();
        void RenderLine(int line);
        void ShowFrame();
    }

	public class NoopRenderer : IRenderer
	{
		public void Initialize(object data)	{}
		public void Reset()					{}
		public void RenderLine(int line)		{}
		public void ShowFrame()				{}
	}
}
